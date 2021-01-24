using BenchmarkDotNet.Running;
using System;

namespace RecordParser.Benchmark
{
    public class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            var runner = new TestRunner();

            runner.SpanCSVIndexedRaw().GetAwaiter().GetResult();
#endif

            BenchmarkRunner.Run<TestRunner>();

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
        public Guid id;
        public string name;
        public int age;
        public DateTime birthday;
        public Gender gender;
        public string email;
        public bool children;
    }
}
