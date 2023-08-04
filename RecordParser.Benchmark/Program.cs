using BenchmarkDotNet.Running;
using System;
using System.Threading.Tasks;

namespace RecordParser.Benchmark
{
    public class Program
    {
        static async Task Main(string[] args)
        {
#if DEBUG
            new VariableLengthReaderBenchmark() { LimitRecord = 500_000 }.Read_VariableLength_RecordParser_Raw(false, false);
#else
            Console.WriteLine("Benchmark options:");
            Console.WriteLine("0 - all");
            Console.WriteLine("1 - variable length writer");
            Console.WriteLine("2 - variable length reader");
            Console.WriteLine("3 - fixed length reader");
            Console.WriteLine("Digit the number of desired benchmark: ");
            var option = int.Parse(Console.ReadLine());

            switch (option)
            {
                case 0:
                    BenchmarkRunner.Run<VariableLengthWriterBenchmark>();
                    BenchmarkRunner.Run<VariableLengthReaderBenchmark>();
                    BenchmarkRunner.Run<FixedLengthReaderBenchmark>();
                    break;

                case 1:
                    BenchmarkRunner.Run<VariableLengthWriterBenchmark>();
                    break;

                case 2:
                    BenchmarkRunner.Run<VariableLengthReaderBenchmark>();
                    break;

                case 3:
                    BenchmarkRunner.Run<FixedLengthReaderBenchmark>();
                    break;

                default:
                    throw new NotSupportedException("invalid option");
            }
#endif
            Console.Out.Write("Hit <enter> to exit...");
            Console.In.ReadLine();
        }
    }

    public enum Gender
    {
        Female = 0,
        Male = 1,
    }

    public struct Person
    {
        public char alfa;
        public Guid? id;
        public string name;
        public int age;
        public DateTime birthday;
        public Gender gender;
        public string email;
        public bool children;
    }

    // TinyCsvParser library limits that our type
    // 1) must be a class
    // 2) must have properties instead of fields

    public class PersonTinyCsvParser
    {
        public char alfa { get; set; }
        public Guid? id { get; set; }
        public string name { get; set; }
        public int age { get; set; }
        public DateTime birthday { get; set; }
        public Gender gender { get; set; }
        public string email { get; set; }
        public bool children { get; set; }
    }

    // SoftCircuits.CsvParser library limits that our type
    // 1) must be a class

    public class PersonSoftCircuitsCsvParser
    {
        public char alfa;
        public Guid? id;
        public string name;
        public int age;
        public DateTime birthday;
        public Gender gender;
        public string email;
        public bool children;
    }
}
