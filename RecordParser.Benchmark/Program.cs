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
            await new TestRunner().FixedLength_Span_Builder();
#else
            BenchmarkRunner.Run<VariableLengthReaderBuilder>();
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
}
