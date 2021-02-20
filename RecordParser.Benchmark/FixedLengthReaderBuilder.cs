using BenchmarkDotNet.Attributes;
using RecordParser.Parsers;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RecordParser.Benchmark
{
    public partial class TestRunner
    {
        public string PathSampleDataTXT => Path.Combine(Directory.GetCurrentDirectory(), "SampleData.txt");

        [Benchmark]
        public async Task FixedLength_String_Raw()
        {
            using var fileStream = File.OpenRead(PathSampleDataTXT);
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, bufferSize: 128);

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
        }

        [Benchmark]
        public async Task FixedLength_Span_Builder()
        {
            var parser = new FixedLengthReaderBuilder<Person>()
                .Map(x => x.alfa, 0, 1)
                .Map(x => x.name, 2, 30)
                .Map(x => x.age, 32, 2)
                .Map(x => x.birthday, 39, 10)
                .Map(x => x.gender, 85, 6)
                .Map(x => x.email, 92, 22)
                .Map(x => x.children, 121, 5)
                .Build();

            await ProcessFlatFile(parser.Parse);
        }

        [Benchmark]
        public async Task FixedLength_Span_Raw()
        {
            await ProcessFlatFile((ReadOnlySpan<char> line) =>
            {
                return new Person
                {
                    alfa = line[0],
                    name = new string(line.Slice(2, 30).Trim()),
                    age = int.Parse(line.Slice(32, 2)),
                    birthday = DateTime.Parse(line.Slice(39, 10), CultureInfo.InvariantCulture),
                    gender = Enum.Parse<Gender>(new string(line.Slice(85, 6))),
                    email = new string(line.Slice(92, 22).Trim()),
                    children = bool.Parse(line.Slice(121, 5))
                };
            });
        }
    }
}
