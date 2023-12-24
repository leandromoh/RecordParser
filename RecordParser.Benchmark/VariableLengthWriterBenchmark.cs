using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CsvHelper.Configuration;
using Cysharp.Text;
using FlatFiles;
using FlatFiles.TypeMapping;
using RecordParser.Builders.Writer;
using RecordParser.Extensions;
using SoftCircuits.CsvParser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RecordParser.Benchmark
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80)]
    public class VariableLengthWriterBenchmark
    {
        [Params(500_000)]
        public int LimitRecord { get; set; }
        public string GetFileName([CallerMemberName] string caller = null) =>
            Path.Combine(Directory.GetCurrentDirectory(), caller + ".csv");

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

#if TEST_ALL
        [Benchmark]
#endif
        public async Task Write_VariableLength_ManualString()
        {
            using var fileStream = File.Create(GetFileName());
            using var streamWriter = new StreamWriter(fileStream);

            var sb = new StringBuilder(100);
            var i = 0;

            while (true)
            {
                foreach (var person in _people)
                {
                    if (i++ == LimitRecord) return;

                    sb.Append(person.id);
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
        [Arguments(false, null)]
        [Arguments(true, true)]
        [Arguments(true, false)]
        public async Task Write_VariableLength_RecordParser_Extension(bool parallel, bool? ordered)
        {
            using var fileStream = File.Create(GetFileName());
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

            var i = 0;

            streamWriter.WriteRecords(Items(), writer.TryFormat, new() 
            { 
                Enabled = parallel, 
                EnsureOriginalOrdering = ordered ?? true,
                MaxDegreeOfParallelism = 4
            });

            IEnumerable<Person> Items()
            {
                while (true)
                {
                    foreach (var person in _people)
                    {
                        if (i++ == LimitRecord)
                            yield break;
                        else
                            yield return person;
                    }
                }
            }
        }

        [Benchmark]
        public async Task Write_VariableLength_RecordParser()
        {
            using var fileStream = File.Create(GetFileName());
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
            var i = 0;

            while (true)
            {
                foreach (var person in _people)
                {
                    if (i++ == LimitRecord) return;

                    if (!writer.TryFormat(person, destination, out charsWritten))
                        throw new Exception("cannot write object");

                    await streamWriter.WriteLineAsync(destination, 0, charsWritten);
                }
            }
        }

#if TEST_ALL
        [Benchmark]
#endif
        public async Task Write_VariableLength_FlatFiles()
        {
            var mapper = DelimitedTypeMapper.Define(() => new PersonSoftCircuitsCsvParser());

            mapper.Property(x => x.id);
            mapper.Property(x => x.name);
            mapper.Property(x => x.age);
            mapper.Property(x => x.birthday);
            mapper.EnumProperty(x => x.gender).Formatter(x => x is Gender.Female ? nameof(Gender.Female) : nameof(Gender.Male));
            mapper.Property(x => x.email);
            mapper.Property(x => x.children);

            var options = new DelimitedOptions { FormatProvider = CultureInfo.InvariantCulture, Separator = ";" };

            using var fileStream = File.Create(GetFileName());
            using var streamWriter = new StreamWriter(fileStream);
            var writer = mapper.GetWriter(streamWriter, options);

            var i = 0;
            while (true)
            {
                foreach (var person in _peopleSoftCircuits)
                {
                    if (i++ == LimitRecord) return;

                    await writer.WriteAsync(person);
                }
            }
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
                Map(x => x.children).Index(6);
            }
        }

#if TEST_ALL
        [Benchmark]
#endif
        public void Write_VariableLength_CSVHelper()
        {
            using var fileStream = File.Create(GetFileName());
            using var writer = new StreamWriter(fileStream);
            using var csvWriter = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);
            csvWriter.Context.RegisterClassMap<PersonMap>();

            var i = 0;

            while (true)
            {
                foreach (var person in _people)
                {
                    if (i++ == LimitRecord) return;

                    csvWriter.WriteRecord(person);
                }
            }
        }

        class PersonMaps : ColumnMaps<PersonSoftCircuitsCsvParser>
        {
            public PersonMaps()
            {
                MapColumn(x => x.id).Index(0);
                MapColumn(x => x.name).Index(1);
                MapColumn(x => x.age).Index(2);
                MapColumn(x => x.birthday).Index(3);
                MapColumn(x => x.gender).Index(4);
                MapColumn(x => x.email).Index(5);
                MapColumn(x => x.children).Index(6);
            }
        }

        private readonly IEnumerable<PersonSoftCircuitsCsvParser> _peopleSoftCircuits = new PersonSoftCircuitsCsvParser[]
        {
            new () { id = new Guid("ec9a8be9-a000-503b-adcf-7266804f1eb1"), name = "Lilly Bradley", age = 21, birthday = new DateTime(1977, 11, 16), gender = Gender.Male, email = "pak@witak.bf", children = true },
            new () { id = new Guid("63858071-cbb3-5abd-9f88-3dfd565cc4ab"), name = "Lucy Berry", age = 49, birthday = new DateTime(1961, 11, 12), gender = Gender.Female, email = "vanvo@ro.pk", children = false },
            new () { id = new Guid("203804f9-93e7-5510-8bb2-177296bafe6a"), name = "Frank Fox", age = 36, birthday = new DateTime(1977, 3, 19), gender = Gender.Male, email = "vav@ped.fj", children = true },

            new () { id = new Guid("a8af66fb-bad4-51eb-810c-bf3ca22337c6"), name = "Isabel Todd", age = 51, birthday = new DateTime(1999, 9, 16), gender = Gender.Female, email = "gu@or.bz", children = false },
            new () { id = new Guid("1a3d8a66-3e0c-50eb-99c1-a3926bce15ed"), name = "Joseph Scott", age = 55, birthday = new DateTime(1986, 10, 26), gender = Gender.Male, email = "bup@vugeb.tt", children = false },
            new () { id = new Guid("aa7d4395-f10f-5776-9912-e3d86c4b9d3c"), name = "Gilbert Brooks", age = 56, birthday = new DateTime(1956, 3, 1), gender = Gender.Female, email = "epiju@ba.ly", children = true },

            new () { id = new Guid("1d25b811-4002-5744-ac40-93a50f2a442c"), name = "Louis Bennett", age = 25, birthday = new DateTime(1967, 4, 4), gender = Gender.Male, email = "ma@itrovive.tv", children = true },
            new () { id = new Guid("8e963ae5-a9ed-5572-b11c-566abc6a8a56"), name = "Norman Parker", age = 57, birthday = new DateTime(1969, 4, 17), gender = Gender.Male, email = "omi@hewepa.bw", children = true },
            new () { id = new Guid("4d373cfb-79e3-54ce-87ff-f2a08fde8f28"), name = "Gary Doyle", age = 1, birthday = new DateTime(1958, 7, 21), gender = Gender.Male, email = "orjohma@cabmofa.ps", children = true },

            new () { id = new Guid("5af00cdf-0758-5317-bcdf-c9a3337cc266"), name = "Bruce Silva", age = 39, birthday = new DateTime(1968, 1, 11), gender = Gender.Female, email = "ta@ovonib.ir", children = true },
        };

#if TEST_ALL
        [Benchmark]
#endif
        public void Write_VariableLength_SoftCircuitsCsvParser()
        {
            var setting = new CsvSettings
            {
                ColumnDelimiter = ','
            };

            using var csvWriter = new CsvWriter<PersonSoftCircuitsCsvParser>(GetFileName(), setting);

            var i = 0;

            while (true)
            {
                foreach (var person in _peopleSoftCircuits)
                {
                    if (i++ == LimitRecord) return;

                    csvWriter.Write(person);
                }
            }
        }

#if TEST_ALL
        [Benchmark]
#endif
        public async Task Write_VariableLength_ZString()
        {
            using var fileStream = File.Create(GetFileName());
            using var streamWriter = new StreamWriter(fileStream);
            using var sb = ZString.CreateStringBuilder();

            var i = 0;

            while (true)
            {
                foreach (var person in _people)
                {
                    if (i++ == LimitRecord) return;

                    sb.Append(person.id);
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

                    var memory = sb.AsMemory();
                    await streamWriter.WriteLineAsync(memory);
                    sb.Clear();
                }
            }
        }
    }
}
