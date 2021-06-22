using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CsvHelper;
using CsvHelper.Configuration;
using FlatFiles;
using FlatFiles.TypeMapping;
using RecordParser.Builders.Reader;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TinyCsvParser;
using TinyCsvParser.Mapping;
using TinyCsvParser.TypeConverter;
using static RecordParser.Benchmark.Common;

namespace RecordParser.Benchmark
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    public class VariableLengthReaderBenchmark
    {
        [Params(500_000)]
        public int LimitRecord { get; set; }

        public string PathSampleDataCSV => Path.Combine(Directory.GetCurrentDirectory(), "SampleData.csv");

        [Benchmark]
        public async Task Read_VariableLength_ManualString()
        {
            using var fileStream = File.OpenRead(PathSampleDataCSV);
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, bufferSize: 128);

            string line;
            var i = 0;

            while ((line = await streamReader.ReadLineAsync()) != null)
            {
                if (i++ == LimitRecord) return;

                var coluns = line.Split(",");
                var person = new Person()
                {
                    id = Guid.Parse(coluns[0]),
                    name = coluns[1].Trim(),
                    age = int.Parse(coluns[2]),
                    birthday = DateTime.Parse(coluns[3], CultureInfo.InvariantCulture),
                    gender = Enum.Parse<Gender>(coluns[4]),
                    email = coluns[5].Trim(),
                    children = bool.Parse(coluns[7])
                };
            }
        }

        [Benchmark]
        public async Task Read_VariableLength_RecordParser()
        {
            var parser = new VariableLengthReaderBuilder<Person>()
                .Map(x => x.id, 0)
                .Map(x => x.name, 1)
                .Map(x => x.age, 2)
                .Map(x => x.birthday, 3)
                .Map(x => x.gender, 4)
                .Map(x => x.email, 5)
                .Map(x => x.children, 7)
                .Build(",", CultureInfo.InvariantCulture);

            await ProcessCSVFile(parser.Parse);
        }

        [Benchmark]
        public async Task Read_VariableLength_FlatFiles()
        {
            var mapper = SeparatedValueTypeMapper.Define(() => new PersonSoftCircuitsCsvParser());

            mapper.Property(x => x.id);
            mapper.Property(x => x.name);
            mapper.Property(x => x.age);
            mapper.Property(x => x.birthday).InputFormat("M/d/yyyy");
            mapper.EnumProperty(x => x.gender);
            mapper.Property(x => x.email);
            mapper.Ignored();
            mapper.Property(x => x.children);

            var options = new SeparatedValueOptions { FormatProvider = CultureInfo.InvariantCulture };

            using var fileStream = File.OpenRead(PathSampleDataCSV);
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, bufferSize: 128);

            var i = 0;
            foreach (var person in mapper.Read(streamReader, options))
            {
                if (i++ == LimitRecord) return;
            }
        }

        [Benchmark]
        public async Task Read_VariableLength_ManualSpan()
        {
            await ProcessCSVFile((ReadOnlySpan<char> line) =>
            {
                var scanned = -1;
                var position = 0;

                var id = ParseChunk(ref line, ref scanned, ref position);
                var name = ParseChunk(ref line, ref scanned, ref position);
                var age = ParseChunk(ref line, ref scanned, ref position);
                var birthday = ParseChunk(ref line, ref scanned, ref position);
                var gender = ParseChunk(ref line, ref scanned, ref position);
                var email = ParseChunk(ref line, ref scanned, ref position);
                ParseChunk(ref line, ref scanned, ref position);
                var children = ParseChunk(ref line, ref scanned, ref position);

                return new Person
                {
                    id = Guid.Parse(id),
                    name = name.ToString(),
                    age = int.Parse(age),
                    birthday = DateTime.Parse(birthday, DateTimeFormatInfo.InvariantInfo),
                    gender = Enum.Parse<Gender>(gender.ToString()),
                    email = email.ToString(),
                    children = bool.Parse(children)
                };
            });
        }

        public class PersonMap : ClassMap<Person>
        {
            public PersonMap()
            {
                Map(x => x.id).Index(0);
                Map(x => x.name).Index(1);
                Map(x => x.age).Index(2);
                Map(x => x.birthday).Index(3);
                Map(x => x.gender).Index(4);
                Map(x => x.email).Index(5);
                Map(x => x.children).Index(7);
            }
        }

        [Benchmark]
        public async Task Read_VariableLength_CSVHelper()
        {
            using var reader = new StreamReader(PathSampleDataCSV);
            using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
            csvReader.Context.RegisterClassMap<PersonMap>();

            var i = 0;
            while (await csvReader.ReadAsync())
            {
                if (i++ == LimitRecord) return;

                var record = csvReader.GetRecord<Person>();
            }
        }

        class PersonTinyCsvMapping : CsvMapping<PersonTinyCsvParser>
        {
            public PersonTinyCsvMapping() : base()
            {
                MapProperty(0, x => x.id);
                MapProperty(1, x => x.name);
                MapProperty(2, x => x.age);
                MapProperty(3, x => x.birthday);
                MapProperty(4, x => x.gender, new EnumConverter<Gender>());
                MapProperty(5, x => x.email);
                MapProperty(7, x => x.children);
            }
        }

        [Benchmark]
        public void Read_VariableLength_TinyCsvParser()
        {
            var csvParserOptions = new CsvParserOptions(true, ',');
            var csvParser = new CsvParser<PersonTinyCsvParser>(csvParserOptions, new PersonTinyCsvMapping());

            var records = csvParser.ReadFromFile(PathSampleDataCSV, Encoding.UTF8);

            var i = 0;
            foreach (var item in records)
            {
                if (i++ == LimitRecord) return;

                var record = item.Result;
            }
        }

        public async Task ProcessCSVFile(FuncSpanT<Person> parser)
        {
            await ProcessFile(PathSampleDataCSV, parser, LimitRecord);
        }
    }
}
