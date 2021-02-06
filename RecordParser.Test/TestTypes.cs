using System;

namespace RecordParser.Test
{
    internal enum Color
    {
        Black,
        White,
        Yellow,
        LightBlue,
    }

    internal class Person
    {
        public DateTime BirthDay { get; set; }
        public string Name { get; set; }

        public Person Mother { get; set; }
    }
}
