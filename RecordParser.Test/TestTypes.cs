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

    internal class AllType
    {
        public string Str;
        public char Char;

        public byte Byte;
        public sbyte SByte;

        public double Double;
        public float Float;

        public int Int;
        public uint UInt;

        public long Long;
        public ulong ULong;

        public short Short;
        public ushort UShort;

        public Guid Guid;
        public DateTime Date;
        public TimeSpan TimeSpan;

        public bool Bool;
        public decimal Decimal;
    }
}
