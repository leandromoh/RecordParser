using RecordParser.Engines.Reader;
using System;
using System.Globalization;

namespace RecordParser.Visitors
{
    internal struct Foo
    {
        ReadOnlyMemory<char> memory;

        public Foo(ReadOnlyMemory<char> memory)
        {
            this.memory = memory;
        }

        public Foo Slice(int index, int count)
        {
            memory = memory.Slice(index, count);
            return this;
        }

        public Foo Slice(int index)
        {
            memory = memory.Slice(index);
            return this;
        }

        public Foo Trim()
        {
#if NET6_0_OR_GREATER
            memory = memory.Trim();
#else
            // TODO
#endif
            return this;
        }

        public T AsCustom<T>(FuncSpanT<T> func) => func(memory.Span);
        public string AsString() => new string(memory.Span);
        public char AsChar() => PrimitiveTypeReaderEngine.ToChar(memory.Span);
        public byte AsByte() => byte.Parse(memory.Span, NumberStyles.Integer, null);
        public sbyte AsSByte() => sbyte.Parse(memory.Span, NumberStyles.Integer, null);
        public double AsDouble() => double.Parse(memory.Span, NumberStyles.AllowThousands | NumberStyles.Float, null);
        public float AsSingle() => float.Parse(memory.Span, NumberStyles.AllowThousands | NumberStyles.Float, null);
        public int AsInt32() => int.Parse(memory.Span, NumberStyles.Integer, null);
        public uint AsUInt32() => uint.Parse(memory.Span, NumberStyles.Integer, null);
        public long AsInt64() => long.Parse(memory.Span, NumberStyles.Integer, null);
        public ulong AsUInt64() => ulong.Parse(memory.Span, NumberStyles.Integer, null);
        public short AsInt16() => short.Parse(memory.Span, NumberStyles.Integer, null);
        public ushort AsUInt16() => ushort.Parse(memory.Span, NumberStyles.Integer, null);
        public Guid AsGuid() => Guid.Parse(memory.Span);
        public DateTime AsDateTime() => DateTime.Parse(memory.Span, null, DateTimeStyles.AllowWhiteSpaces);
        public TimeSpan AsTimeSpan() => TimeSpan.Parse(memory.Span, null);
        public bool AsBoolean() => bool.Parse(memory.Span);
        public decimal AsDecimal() => decimal.Parse(memory.Span, NumberStyles.Number, null);
        public TEnum AsEnum<TEnum>() where TEnum : struct
        {
#if NET6_0_OR_GREATER
            return Enum.Parse<TEnum>(memory.Span, ignoreCase: true);
#else
            return Enum.Parse<TEnum>(memory.Span.ToString(), ignoreCase: true);
#endif
        }
    }

    //internal class SpanReplacerVisitor : ExpressionVisitor
    //{
    //    private readonly static ParameterExpression memory = Expression.Parameter(typeof(Foo), "memory");

    //    public Expression<Func<Foo, T>> Modify<T>(Expression<FuncSpanT<T>> ex)
    //    {
    //        if (ex is null) return null;

    //        var body = Visit(ex.Body);

    //        body = new StaticMethodVisitor().Visit(body);

    //        var lamb = Expression.Lambda<Func<Foo, T>>(body, memory);

    //        return lamb;
    //    }

    //    protected override Expression VisitBinary(BinaryExpression node)
    //    {
    //        if (node.NodeType != ExpressionType.Assign)
    //            return base.VisitBinary(node);

    //        if (node.Right is MethodCallExpression method)
    //        {
    //            var resolve 

    //            var origin = method.Method.DeclaringType;
    //            if (origin.Assembly == typeof(object).Assembly)
    //            {
    //                Expression.Call(memory, )
    //            }
    //        }

    //        return base.VisitBinary(node);
    //    }

        //protected override Expression VisitParameter(ParameterExpression node)
        //{
        //    if (node.Type == typeof(ReadOnlySpan<char>))
        //        return span;

        //    return base.VisitParameter(node);
        //}

        //class StaticMethodVisitor : ExpressionVisitor
        //{
        //    public static string ToString(ReadOnlySpan<char> span) => span.ToString();
        //    public static ReadOnlySpan<char> Trim(ReadOnlySpan<char> span) => span.Trim();
        //    public static ReadOnlySpan<char> Slice1(ReadOnlySpan<char> span, int start) => span.Slice(start);
        //    public static ReadOnlySpan<char> Slice2(ReadOnlySpan<char> span, int start, int count) => span.Slice(start, count);

        //    protected override Expression VisitMethodCall(MethodCallExpression node)
        //    {
        //        if (node.Object?.Type == typeof(ReadOnlySpan<char>))
        //        {
        //            var args = node.Arguments.Prepend(node.Object).ToArray();

        //            if (node.Method.Name == "Slice")
        //            {
        //                Delegate f = node.Arguments.Count == 1
        //                    ? StaticMethodVisitor.Slice1
        //                    : StaticMethodVisitor.Slice2;

        //                return Expression.Call(f.Method, args);
        //            }

        //            if (node.Method.Name == "ToString")
        //            {
        //                var f = StaticMethodVisitor.ToString;

        //                return Expression.Call(f.Method, args);
        //            }

        //            if (node.Method.Name == "Trim")
        //            {
        //                var f = StaticMethodVisitor.Trim;

        //                return Expression.Call(f.Method, args);
        //            }
        //        }

        //        return base.VisitMethodCall(node);
        //    }
        //}
    //}
}
