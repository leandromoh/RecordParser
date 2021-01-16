using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.BuilderWrite
{
    public class CSVWriteBuilder<T>
    {
        private readonly Dictionary<int, MappingWriteConfiguration> list = new Dictionary<int, MappingWriteConfiguration>();
        private FuncSpanSpanIntT<T> func;

        public CSVWriteBuilder<T> Map<R>(Expression<Func<T, R>> ex, int collumn, string format = null, IFormatProvider formatProvider = null)
        {
            var member = ex.Body as MemberExpression ?? throw new ArgumentException("Must be member expression", nameof(ex));
            var config = new MappingWriteConfiguration(member, collumn, format, typeof(R), formatProvider);
            list.Add(collumn, config);
            return this;
        }

        public CSVWriteBuilder<T> Build()
        {
            var maps = list.Select(x => x.Value);
            var expression = GetFuncThatSetProperties<T>(maps);
            func = expression.Compile();
            return this;
        }

        public ReadOnlySpan<char> Parse(T bla)
        {
            Span<char> span = stackalloc char[100];
            var lineLength = func(span, " ; ", bla);
            Span<char> line = new char[lineLength];
            span.Slice(0, lineLength).CopyTo(line);
            return line;
        }

        delegate int FuncSpanSpanIntT<X>(Span<char> span, ReadOnlySpan<char> delimiter, X inst);

        private static Expression<FuncSpanSpanIntT<X>> GetFuncThatSetProperties<X>(IEnumerable<MappingWriteConfiguration> mappedColumns)
        {
            // parameters
            ParameterExpression span = Expression.Variable(typeof(Span<char>), "span");
            ParameterExpression delimiter = Expression.Variable(typeof(ReadOnlySpan<char>), "delimiter");
            ParameterExpression inst = Expression.Variable(typeof(X), "inst");

            var replacer = new ParameterReplacer(inst);

            // variables
            ParameterExpression offset = Expression.Variable(typeof(int), "offset");
            ParameterExpression position = Expression.Variable(typeof(int), "position");
            ParameterExpression delimiterLength = Expression.Variable(typeof(int), "delimiterLength");
            List<ParameterExpression> variables = new List<ParameterExpression>();
            variables.Add(offset);
            variables.Add(position);
            variables.Add(delimiterLength);

            // commands
            List<Expression> commands = new List<Expression>();
            commands.Add(
                Expression.Assign(delimiterLength, Expression.PropertyOrField(delimiter, "Length")));

            commands.Add(
                Expression.Assign(
                    offset,
                    Expression.Subtract(
                        Expression.Constant(0),
                        delimiterLength)));

            commands.Add(Expression.Assign(position, Expression.Constant(0)));

            var i = 0;
            foreach (var map in mappedColumns)
            {
                var prop = replacer.Visit(map.prop);

                ParameterExpression spanTemp = Expression.Variable(typeof(Span<char>), $"span{++i}");
                variables.Add(spanTemp);

                commands.Add(
                    Expression.Assign(spanTemp, Expression.Call(span, "Slice", Type.EmptyTypes, position)));


                if (prop.Type == typeof(string))
                {
                    var strSpan = StringAsSpan(prop);

                    commands.Add(
                        Expression.Call(strSpan, "CopyTo", Type.EmptyTypes, spanTemp));

                    commands.Add(
                        Expression.Assign(offset, Expression.PropertyOrField(prop, "Length")));
                }
                else
                {
                    var format = map.format is null
                        ? Expression.Default(typeof(ReadOnlySpan<char>))
                        : StringAsSpan(Expression.Constant(map.format));

                    commands.Add(
                        Expression.Call(prop, "TryFormat", Type.EmptyTypes, 
                        spanTemp, offset, format, Expression.Constant(map.formatProvider, typeof(IFormatProvider))));
                }

                commands.Add(
                    Expression.Call(delimiter, "CopyTo", Type.EmptyTypes, Expression.Call(spanTemp, "Slice", Type.EmptyTypes, offset)));

                commands.Add(
                    Expression.AddAssign(position, Expression.Add(offset, delimiterLength)));

            }

            commands.Add(Expression.Subtract(position, delimiterLength));

            var blockExpr = Expression.Block(variables, commands);

            var lambda = Expression.Lambda<FuncSpanSpanIntT<X>>(blockExpr, new[] { span, delimiter, inst });

            return lambda;

            Expression StringAsSpan(Expression str) => Expression.Call(typeof(MemoryExtensions), nameof(MemoryExtensions.AsSpan), Type.EmptyTypes, str);
        }
    }

    public readonly struct MappingWriteConfiguration
    {
        public MemberExpression prop { get; }
        public int start { get; }
        public string format { get; }
        public IFormatProvider formatProvider { get; }
        public Type type { get; }

        public MappingWriteConfiguration(MemberExpression prop, int start, string format, Type type, IFormatProvider formatProvider)
        {
            this.prop = prop;
            this.start = start;
            this.format = format;
            this.type = type;
            this.formatProvider = formatProvider;
        }
    }
}
