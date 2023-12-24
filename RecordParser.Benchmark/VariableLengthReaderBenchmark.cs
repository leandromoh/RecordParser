using Ben.Collections.Specialized;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CsvHelper;
using CsvHelper.Configuration;
using Cursively;
using FlatFiles;
using FlatFiles.TypeMapping;
using RecordParser.Builders.Reader;
using RecordParser.Extensions;
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
    [SimpleJob(RuntimeMoniker.Net80)]
    public class VariableLengthReaderBenchmark
    {
        [Params(500_000)]
        public int LimitRecord { get; set; }

        public string PathSampleDataCSV => Path.Combine(Directory.GetCurrentDirectory(), "SampleData.csv");

        public string PathSampleDataQuotedCSV => Path.Combine(Directory.GetCurrentDirectory(), "SampleDataQuoted.csv");

#if TEST_ALL
        [Benchmark]
#endif
        public async Task Read_VariableLength_ManualString()
        {
            using var fileStream = File.OpenRead(PathSampleDataCSV);
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize);

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

            if (i != LimitRecord)
                throw new Exception($"read {i} records but expected {LimitRecord}");
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
        [Arguments(false, true)]
        [Arguments(true, true)]
        public void Read_VariableLength_FullQuoted_RecordParser_Parallel(bool parallel, bool quoted)
        {
            var builder = new VariableLengthReaderBuilder<Person>()
                .Map(x => x.id, 0)
                .Map(x => x.name, 1)
                .Map(x => x.age, 2)
                .Map(x => x.birthday, 3)
                .Map(x => x.gender, 4)
                .Map(x => x.email, 5)
                .Map(x => x.children, 7);

            if (parallel == false)
                builder.DefaultTypeConvert(new InternPool().Intern);

            var parser = builder.Build(",", CultureInfo.InvariantCulture);

            using var fileStream = File.OpenRead(PathSampleDataQuotedCSV);
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize);

            var readOptions = new VariableLengthReaderOptions
            {
                HasHeader = false,
                ParallelismOptions = new() { Enabled = parallel },
                ContainsQuotedFields = quoted,
            };

            var items = streamReader.ReadRecords(parser, readOptions);

            var i = 0;
            foreach (var person in items)
            {
                if (i++ == LimitRecord) return;
            }

            if (i != LimitRecord)
                throw new Exception($"read {i} records but expected {LimitRecord}");
        }

        [Benchmark]
        [Arguments(true, true)]
        [Arguments(true, false)]
        [Arguments(false, true)]
        [Arguments(false, false)]
        public void Read_VariableLength_RecordParser_Parallel(bool parallel, bool quoted)
        {
            var builder = new VariableLengthReaderBuilder<Person>()
                .Map(x => x.id, 0)
                .Map(x => x.name, 1)
                .Map(x => x.age, 2)
                .Map(x => x.birthday, 3)
                .Map(x => x.gender, 4)
                .Map(x => x.email, 5)
                .Map(x => x.children, 7);

            if (parallel == false)
                builder.DefaultTypeConvert(new InternPool().Intern);

            var parser = builder.Build(",", CultureInfo.InvariantCulture);

            using var fileStream = File.OpenRead(PathSampleDataCSV);
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize);

            var readOptions = new VariableLengthReaderOptions
            {
                HasHeader = false,
                ParallelismOptions = new() { Enabled = parallel },
                ContainsQuotedFields = quoted,
            };

            var items = streamReader.ReadRecords(parser, readOptions);

            var i = 0;
            foreach (var person in items)
            {
                if (i++ == LimitRecord) return;
            }

            if (i != LimitRecord)
                throw new Exception($"read {i} records but expected {LimitRecord}");
        }

        [Benchmark]
        [Arguments(true, true)]
        [Arguments(true, false)]
        [Arguments(false, true)]
        [Arguments(false, false)]
        public void Read_VariableLength_RecordParser_Raw(bool parallel, bool quoted)
        {
            using var fileStream = File.OpenRead(PathSampleDataCSV);
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize);

            var readOptions = new VariableLengthReaderRawOptions
            {
                HasHeader = false,
                ParallelismOptions = new() { Enabled = parallel },
                ContainsQuotedFields = quoted,

                ColumnCount = 8,
                Separator = ",",
                StringPoolFactory = () => new InternPool().Intern
            };

            var items = streamReader.ReadRecordsRaw(readOptions, PersonFactory);

            var i = 0;
            foreach (var person in items)
            {
                if (i++ == LimitRecord) return;
            }

            if (i != LimitRecord)
                throw new Exception($"read {i} records but expected {LimitRecord}");

            Person PersonFactory(Func<int, string> getColumnValue)
            {
                return new Person()
                {
                    id = Guid.Parse(getColumnValue(0)),
                    name = getColumnValue(1).Trim(),
                    age = int.Parse(getColumnValue(2)),
                    birthday = DateTime.Parse(getColumnValue(3), CultureInfo.InvariantCulture),
                    gender = Enum.Parse<Gender>(getColumnValue(4)),
                    email = getColumnValue(5).Trim(),
                    children = bool.Parse(getColumnValue(7))
                };
            }
        }

#if TEST_ALL
        [Benchmark]
#endif
        public async Task Read_VariableLength_FlatFiles()
        {
            var mapper = DelimitedTypeMapper.Define(() => new PersonSoftCircuitsCsvParser());

            mapper.Property(x => x.id);
            mapper.Property(x => x.name);
            mapper.Property(x => x.age);
            mapper.Property(x => x.birthday).InputFormat("M/d/yyyy");
            mapper.EnumProperty(x => x.gender);
            mapper.Property(x => x.email);
            mapper.Ignored();
            mapper.Property(x => x.children);

            var options = new DelimitedOptions { FormatProvider = CultureInfo.InvariantCulture };

            using var fileStream = File.OpenRead(PathSampleDataCSV);
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize);

            var i = 0;
            foreach (var person in mapper.Read(streamReader, options))
            {
                if (i++ == LimitRecord) return;
            }

            if (i != LimitRecord)
                throw new Exception($"read {i} records but expected {LimitRecord}");
        }

#if TEST_ALL
        [Benchmark]
#endif
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
                    gender = Enum.Parse<Gender>(gender),
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

#if TEST_ALL
        [Benchmark]
#endif
        public async Task Read_VariableLength_CSVHelper()
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                CacheFields = true,
                HasHeaderRecord = false,
                BufferSize = BufferSize
            };

            using var reader = new StreamReader(PathSampleDataCSV);
            using var csvReader = new CsvReader(reader, config);
            csvReader.Context.RegisterClassMap<PersonMap>();

            var i = 0;
            while (await csvReader.ReadAsync())
            {
                if (i++ == LimitRecord) return;

                var record = csvReader.GetRecord<Person>();
            }

            if (i != LimitRecord)
                throw new Exception($"read {i} records but expected {LimitRecord}");
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

#if TEST_ALL
        [Benchmark]
#endif
        public void Read_VariableLength_TinyCsvParser()
        {
            var csvParserOptions = new CsvParserOptions(skipHeader: false, ',');
            var csvParser = new CsvParser<PersonTinyCsvParser>(csvParserOptions, new PersonTinyCsvMapping());

            var records = csvParser.ReadFromFile(PathSampleDataCSV, Encoding.UTF8);

            var i = 0;
            foreach (var item in records)
            {
                if (i++ == LimitRecord) return;

                var record = item.Result;
            }

            if (i != LimitRecord)
                throw new Exception($"read {i} records but expected {LimitRecord}");
        }

        private sealed class AllDoneException : Exception { }

#if TEST_ALL
        [Benchmark]
#endif
        public async Task Read_VariableLength_Cursively_Async()
        {
            using FileStream fileStream = new(PathSampleDataCSV, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan | FileOptions.Asynchronous);

            int i = 0;
            CursivelyPersonVisitor visitor = new(OnPersonVisited);
            try
            {
                await CsvAsyncInput
                    .ForStream(fileStream)
                    .ProcessAsync(visitor)
                    .ConfigureAwait(false);
            }
            catch (AllDoneException)
            {
            }

            if (i != LimitRecord)
                throw new Exception($"read {i} records but expected {LimitRecord}");

            void OnPersonVisited(in Person person)
            {
                if (i++ == LimitRecord) { throw new AllDoneException(); }

                // this is where we would do any actual processing.
            }
        }

        [Benchmark]
        public void Read_VariableLength_Cursively_Sync()
        {
            using FileStream fileStream = new(PathSampleDataCSV, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan | FileOptions.Asynchronous);

            int i = 0;
            CursivelyPersonVisitor visitor = new(OnPersonVisited);
            try
            {
                CsvSyncInput
                    .ForStream(fileStream)
                    .Process(visitor);
            }
            catch (AllDoneException)
            {
            }

            if (i != LimitRecord)
                throw new Exception($"read {i} records but expected {LimitRecord}");

            void OnPersonVisited(in Person person)
            {
                if (i++ == LimitRecord) { throw new AllDoneException(); }

                // this is where we would do any actual processing.
            }
        }

        public async Task ProcessCSVFile(FuncSpanT<Person> parser)
        {
            await ProcessFile(PathSampleDataCSV, parser, LimitRecord);
        }
    }
}
