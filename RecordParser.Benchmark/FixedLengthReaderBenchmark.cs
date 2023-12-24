using Ben.Collections.Specialized;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using FlatFiles;
using FlatFiles.TypeMapping;
using RecordParser.Builders.Reader;
using RecordParser.Extensions;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static RecordParser.Benchmark.Common;

namespace RecordParser.Benchmark
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80)]
    public class FixedLengthReaderBenchmark
    {
        [Params(400_000)]
        public int LimitRecord { get; set; }

        public string PathSampleDataTXT => Path.Combine(Directory.GetCurrentDirectory(), "SampleData.txt");

#if TEST_ALL
        [Benchmark]
#endif
        public async Task Read_FixedLength_ManualString()
        {
            using var fileStream = File.OpenRead(PathSampleDataTXT);
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize);

            string line;
            var i = 0;

            while ((line = await streamReader.ReadLineAsync()) != null)
            {
                if (i++ == LimitRecord) return;

                var person = new Person()
                {
                    alfa = line[0],
                    name = line.Substring(2, 30).Trim(),
                    age = int.Parse(line.Substring(32, 2)),
                    birthday = DateTime.Parse(line.Substring(39, 10), CultureInfo.InvariantCulture),
                    gender = Enum.Parse<Gender>(line.Substring(85, 6)),
                    email = line.Substring(92, 22).Trim(),
                    children = bool.Parse(line.Substring(121, 5))
                };
            }

            if (i != LimitRecord)
                throw new Exception($"read {i} records but expected {LimitRecord}");
        }

        [Benchmark]
        public async Task Read_FixedLength_RecordParser()
        {
            var parser = new FixedLengthReaderBuilder<Person>()
                .Map(x => x.alfa, 0, 1)
                .Map(x => x.name, 2, 30)
                .Map(x => x.age, 32, 2)
                .Map(x => x.birthday, 39, 10)
                .Map(x => x.gender, 85, 6)
                .Map(x => x.email, 92, 22)
                .Map(x => x.children, 121, 5)
                .Build(CultureInfo.InvariantCulture);

            await ProcessFlatFile(parser.Parse);
        }

        [Benchmark]
        [Arguments(true)]
        [Arguments(false)]
        public void Read_FixedLength_RecordParser_Parallel(bool parallel)
        {
            var builder = new FixedLengthReaderBuilder<Person>()
                .Map(x => x.alfa, 0, 1)
                .Map(x => x.name, 2, 30)
                .Map(x => x.age, 32, 2)
                .Map(x => x.birthday, 39, 10)
                .Map(x => x.gender, 85, 6)
                .Map(x => x.email, 92, 22)
                .Map(x => x.children, 121, 5);

            if (parallel == false)
                builder.DefaultTypeConvert(new InternPool().Intern);

            var parser = builder.Build(CultureInfo.InvariantCulture);

            using var fileStream = File.OpenRead(PathSampleDataTXT);
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize);

            var readOptions = new FixedLengthReaderOptions<Person>
            {
                ParallelismOptions = new () { Enabled = parallel },
                Parser = parser.Parse,
            };

            var items = streamReader.ReadRecords(readOptions);

            var i = 0;
            foreach (var person in items)
            {
                if (i++ == LimitRecord) return;
            }

            if (i != LimitRecord)
                throw new Exception($"read {i} records but expected {LimitRecord}");
        }

        [Benchmark]
        public void Read_FixedLength_RecordParser_GetLines()
        {
            var parser = new FixedLengthReaderBuilder<Person>()
                .Map(x => x.alfa, 0, 1)
                .Map(x => x.name, 2, 30)
                .Map(x => x.age, 32, 2)
                .Map(x => x.birthday, 39, 10)
                .Map(x => x.gender, 85, 6)
                .Map(x => x.email, 92, 22)
                .Map(x => x.children, 121, 5)
                .DefaultTypeConvert(new InternPool().Intern)
                .Build(CultureInfo.InvariantCulture);

            using var fileStream = File.OpenRead(PathSampleDataTXT);
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize);

            var lines = streamReader.ReadRecords();

            var i = 0;
            foreach (var line in lines)
            {
                if (i++ == LimitRecord) return;
                var person = parser.Parse(line.Span);
            }

            if (i != LimitRecord)
                throw new Exception($"read {i} records but expected {LimitRecord}");
        }

        [Benchmark]
        public async Task Read_FixedLength_ManualSpan()
        {
            var i = 0;
            await ProcessFlatFile((ReadOnlySpan<char> line) =>
            {
                i++;

                return new Person
                {
                    alfa = line[0],
                    name = new string(line.Slice(2, 30).Trim()),
                    age = int.Parse(line.Slice(32, 2)),
                    birthday = DateTime.Parse(line.Slice(39, 10), CultureInfo.InvariantCulture),
                    gender = Enum.Parse<Gender>(line.Slice(85, 6)),
                    email = new string(line.Slice(92, 22).Trim()),
                    children = bool.Parse(line.Slice(121, 5))
                };
            });

            if (i != LimitRecord)
                throw new Exception($"read {i} records but expected {LimitRecord}");
        }

#if TEST_ALL
        [Benchmark]
#endif
        public async Task Read_FixedLength_FlatFiles()
        {
            var mapper = FixedLengthTypeMapper.Define(() => new PersonSoftCircuitsCsvParser());

            mapper.Property(x => x.alfa, 1);
            mapper.Ignored(1);
            mapper.Property(x => x.name, 30);
            mapper.Property(x => x.age, 2);
            mapper.Ignored(5);
            mapper.Property(x => x.birthday, 10).InputFormat("M/d/yyyy");
            mapper.Ignored(36);
            mapper.EnumProperty(x => x.gender, 6);
            mapper.Ignored(1);
            mapper.Property(x => x.email, 22);
            mapper.Ignored(7);
            mapper.Property(x => x.children, 5);

            var options = new FixedLengthOptions { FormatProvider = CultureInfo.InvariantCulture };

            using var fileStream = File.OpenRead(PathSampleDataTXT);
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize);

            var i = 0;
            foreach (var person in mapper.Read(streamReader, options))
            {
                if (i++ == LimitRecord) return;
            }

            if (i != LimitRecord)
                throw new Exception($"read {i} records but expected {LimitRecord}");
        }

        private async Task ProcessFlatFile(FuncSpanT<Person> parser)
        {
            await ProcessFile(PathSampleDataTXT, parser, LimitRecord);
        }
    }
}
