﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using RecordParser.BuilderWrite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RecordParser.Benchmark
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    public partial class WriterTestRunner
    {
        private readonly IEnumerable<Person> _people = LoadPeople();

        private static IEnumerable<Person> LoadPeople()
        {
            return new Person[]
            {
                new Person { id = new Guid("ec9a8be9-a000-503b-adcf-7266804f1eb1"), name = "Lilly Bradley", age = 21, birthday = new DateTime(1977, 11, 16), gender = Gender.Male, email = "pak@witak.bf", children = true },
                new Person { id = new Guid("63858071-cbb3-5abd-9f88-3dfd565cc4ab"), name = "Lucy Berry", age = 49, birthday = new DateTime(1961, 11, 12), gender = Gender.Female, email = "vanvo@ro.pk", children = false },
                new Person { id = new Guid("203804f9-93e7-5510-8bb2-177296bafe6a"), name = "Frank Fox", age = 36, birthday = new DateTime(1977, 3, 19), gender = Gender.Male, email = "vav@ped.fj", children = true },
                
                new Person { id = new Guid("a8af66fb-bad4-51eb-810c-bf3ca22337c6"), name = "Isabel Todd", age = 51, birthday = new DateTime(1999, 9, 16), gender = Gender.Female, email = "gu@or.bz", children = false },
                new Person { id = new Guid("1a3d8a66-3e0c-50eb-99c1-a3926bce15ed"), name = "Joseph Scott", age = 55, birthday = new DateTime(1986, 10, 26), gender = Gender.Male, email = "bup@vugeb.tt", children = false },
                new Person { id = new Guid("aa7d4395-f10f-5776-9912-e3d86c4b9d3c"), name = "Gilbert Brooks", age = 56, birthday = new DateTime(1956, 3, 1), gender = Gender.Female, email = "epiju@ba.ly", children = true },
                
                new Person { id = new Guid("1d25b811-4002-5744-ac40-93a50f2a442c"), name = "Louis Bennett", age = 25, birthday = new DateTime(1967, 4, 4), gender = Gender.Male, email = "ma@itrovive.tv", children = true },
                new Person { id = new Guid("8e963ae5-a9ed-5572-b11c-566abc6a8a56"), name = "Norman Parker", age = 57, birthday = new DateTime(1969, 4, 17), gender = Gender.Male, email = "omi@hewepa.bw", children = true },
                new Person { id = new Guid("4d373cfb-79e3-54ce-87ff-f2a08fde8f28"), name = "Gary Doyle", age = 1, birthday = new DateTime(1958, 7, 21), gender = Gender.Male, email = "orjohma@cabmofa.ps", children = true },
                
                new Person { id = new Guid("5af00cdf-0758-5317-bcdf-c9a3337cc266"), name = "Bruce Silva", age = 39, birthday = new DateTime(1968, 1, 11), gender = Gender.Female, email = "ta@ovonib.ir", children = true },
            };
        }

        public string DesktopPath => Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public string WriteString_PathSampleDataCSV => Path.Combine(DesktopPath, "Write-String.csv");
        public string WriteSpan_PathSampleDataCSV => Path.Combine(DesktopPath, "Write-Span.csv");

        public int LimitLineWrite = 100_000;

        [Benchmark]
        public async Task VariableLength_Write_String_Raw()
        {
            using var fileStream = File.Create(WriteString_PathSampleDataCSV);
            using var streamWriter = new StreamWriter(fileStream);

            var sb = new StringBuilder(100);

            for (var i = 0; i < LimitLineWrite; i++)
            {
                foreach (var person in _people)
                {
                    if (person.id.HasValue)
                        sb.Append(person.id.Value);

                    sb.Append(";");
                    sb.Append(person.name);
                    sb.Append(";");
                    sb.Append(person.age);
                    sb.Append(";");
                    sb.Append(person.birthday);
                    sb.Append(";");
                    sb.Append(person.gender);
                    sb.Append(";");
                    sb.Append(person.email);
                    sb.Append(";");
                    sb.Append(person.children);

                    await streamWriter.WriteLineAsync(sb);
                    sb.Clear();
                }
            }
        }

        [Benchmark]
        public async Task VariableLength_Write_Span_Builder()
        {
            using var fileStream = File.Create(WriteSpan_PathSampleDataCSV);
            using var streamWriter = new StreamWriter(fileStream);

            var writer = new VariableLengthWriterSequentialBuilder<Person>()
                .Map(x => x.id)
                .Map(x => x.name)
                .Map(x => x.age)
                .Map(x => x.birthday)
                .Map(x => x.gender)
                .Map(x => x.email)
                .Map(x => x.children)
                .Build(";");

            var destination = new char[100];

            var charsWritten = 0;

            for (var i = 0; i < LimitLineWrite; i++)
            {
                foreach (var person in _people)
                {
                    if (!writer.Parse(person, destination, out charsWritten))
                        throw new Exception("cannot write object");

                    await streamWriter.WriteLineAsync(destination, 0, charsWritten);
                }
            }
        }
    }
}