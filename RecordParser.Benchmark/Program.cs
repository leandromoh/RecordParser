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
            new VariableLengthReaderBenchmark() { LimitRecord = 500_000 }.Read_VariableLength_FullQuoted_RecordParser_Parallel(false, true);
#else
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run();
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
