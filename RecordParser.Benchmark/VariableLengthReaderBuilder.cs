using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CsvHelper;
using CsvHelper.Configuration;
using RecordParser.Builders.Reader;
using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using TinyCsvParser;
using TinyCsvParser.Mapping;
using TinyCsvParser.TypeConverter;

namespace RecordParser.Benchmark
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    public partial class ReaderTestRunner
    {
        [Params(500_000)]
        public int LimitRecord { get; set; }

        public string PathSampleDataCSV => Path.Combine(Directory.GetCurrentDirectory(), "SampleData.csv");

        [Benchmark]
        public async Task VariableLength_String_Raw()
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
        public async Task VariableLength_Span_Builder()
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
        public async Task VariableLength_Span_Raw()
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
        public async Task Read_VariableLength_TinyCsvParser()
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

        public async Task ProcessFlatFile(FuncSpanT<Person> parser)
        {
            await ProcessFile(parser, PathSampleDataTXT);
        }

        public async Task ProcessCSVFile(FuncSpanT<Person> parser)
        {
            await ProcessFile(parser, PathSampleDataCSV);
        }

        public async Task ProcessFile(FuncSpanT<Person> parser, string filePath)
        {
            using var stream = File.OpenRead(filePath);
            PipeReader reader = PipeReader.Create(stream);

            var i = 0;

            while (true)
            {
                ReadResult read = await reader.ReadAsync();
                ReadOnlySequence<byte> buffer = read.Buffer;
                while (TryReadLine(ref buffer, out ReadOnlySequence<byte> sequence))
                {
                    if (i++ == LimitRecord) return;

                    var person = ProcessSequence(sequence, parser);
                }

                reader.AdvanceTo(buffer.Start, buffer.End);
                if (read.IsCompleted)
                {
                    break;
                }
            }
        }

        static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
        {
            var position = buffer.PositionOf((byte)'\n');
            if (position == null)
            {
                line = default;
                return false;
            }

            line = buffer.Slice(0, position.Value);
            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));

            return true;
        }

        static T ProcessSequence<T>(ReadOnlySequence<byte> sequence, FuncSpanT<T> parser)
        {
            const int LengthLimit = 256;

            if (sequence.IsSingleSegment)
            {
                return Parse(sequence.FirstSpan, parser);
            }

            var length = (int)sequence.Length;
            if (length > LengthLimit)
            {
                throw new ArgumentException($"Line has a length exceeding the limit: {length}");
            }

            Span<byte> span = stackalloc byte[(int)sequence.Length];
            sequence.CopyTo(span);

            return Parse(span, parser);
        }

        static T Parse<T>(ReadOnlySpan<byte> bytes, FuncSpanT<T> parser)
        {
            Span<char> chars = stackalloc char[bytes.Length];
            Encoding.UTF8.GetChars(bytes, chars);

            return parser(chars);
        }

        static ReadOnlySpan<char> ParseChunk(ref ReadOnlySpan<char> span, ref int scanned, ref int position)
        {
            scanned += position + 1;

            position = span.Slice(scanned, span.Length - scanned).IndexOf(',');
            if (position < 0)
            {
                position = span.Slice(scanned, span.Length - scanned).Length;
            }

            return span.Slice(scanned, position);
        }
    }
}
