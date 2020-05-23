using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RecordParser
{
    public class GenericRecordParser<T>
        where T : class
    {
        private readonly IReadOnlyDictionary<string, Action<T, string>> _setterByProperty;
        private readonly IEnumerable<string> _mappedColumns;
        private readonly HashSet<string> _columnsThatIfEmptyShouldParseObjectAsNull;
        private readonly Func<T> _getNewInstance;

        public GenericRecordParser(string[] mappedColumns, string[] columnsThatIfEmptyShouldObjectParseAsNull = null)
        {
            _mappedColumns = mappedColumns;
            _setterByProperty = FillSetterByProperty(mappedColumns);
            _columnsThatIfEmptyShouldParseObjectAsNull = columnsThatIfEmptyShouldObjectParseAsNull?.ToHashSet()
                                                              ?? new HashSet<string>();

            foreach (var skipColumn in _columnsThatIfEmptyShouldParseObjectAsNull)
            {
                if (!_setterByProperty.TryGetValue(skipColumn, out var _))
                {
                    throw new ArgumentException($"Property {skipColumn} is not mapped in mappedColumns argument");
                }
            }

            _getNewInstance = CreateInstanceHelper.GetInstanceGenerator<T>(_mappedColumns);
        }

        public T Parse(string line)
        {
            return Parse(_getNewInstance(), line);
        }

        public T Parse(T obj, string line)
        {
            foreach ((var propertyName, var value) in _mappedColumns.Zip(line.Split(';'), ValueTuple.Create))
            {
                if (propertyName is null)
                    continue;

                if (string.IsNullOrWhiteSpace(value) && _columnsThatIfEmptyShouldParseObjectAsNull.Contains(propertyName))
                    return null;

                _setterByProperty[propertyName](obj, value);
            }

            return obj;
        }

        private static Dictionary<string, Action<T, string>> FillSetterByProperty(IEnumerable<string> mappedColumns)
        {
            var setterByProperty = new Dictionary<string, Action<T, string>>();

            ParameterExpression objectParameter = Expression.Variable(typeof(T), "x");
            ParameterExpression valueParameter = Expression.Variable(typeof(string), "value");

            MethodInfo isNullOrWhiteSpaceMethodInfo = GetIsNullOrWhiteSpaceMethodInfo();

            foreach (var propertyName in mappedColumns)
            {
                if (propertyName is null)
                    continue;

                var propertyType = propertyName
                    .Split('.')
                    .Aggregate(typeof(T), (type, member) => type.GetProperty(member).PropertyType);

                var isPropertyNullable = propertyType.IsGenericType &&
                                         propertyType.GetGenericTypeDefinition() == typeof(Nullable<>);

                var propertyUnderlyingType = isPropertyNullable
                                                ? propertyType.GetGenericArguments()[0]
                                                : propertyType;

                Expression valueToBeSetExpression = GetValueToBeSetExpression(
                                                        propertyUnderlyingType,
                                                        valueParameter);

                if (isPropertyNullable)
                {
                    valueToBeSetExpression =
                        WrapWithTernaryThatReturnsNullIfValueIsEmpty(
                            valueToBeSetExpression,
                            valueParameter,
                            propertyType,
                            isNullOrWhiteSpaceMethodInfo);
                }

                setterByProperty[propertyName] = GetSetterFunction(
                                                    valueToBeSetExpression,
                                                    objectParameter,
                                                    valueParameter,
                                                    propertyName);
            }

            return setterByProperty;
        }

        private static Expression GetValueToBeSetExpression(Type propertyType, ParameterExpression valueParameter)
        {
            if (propertyType.IsEnum)
                return GetEnumParseExpression(valueParameter, propertyType);

            return GetParseExpression(propertyType, valueParameter);
        }

        private static Action<T, string> GetSetterFunction(
            Expression valueToBeSetExpression,
            ParameterExpression objectParameter,
            ParameterExpression valueParameter,
            string propertyName)
        {
            var memberExpression = propertyName
                .Split('.')
                .Aggregate((Expression)objectParameter, (body, member) => Expression.PropertyOrField(body, member));

            BinaryExpression assignExpression = Expression.Assign(memberExpression, valueToBeSetExpression);
            ParameterExpression[] parametersExpression = new[] { objectParameter, valueParameter };

            return
                Expression
                    .Lambda<Action<T, string>>(
                        assignExpression,
                        parametersExpression)
                    .Compile();
        }

        private static Expression WrapWithTernaryThatReturnsNullIfValueIsEmpty(
            Expression valueToBeSetExpression,
            ParameterExpression valueParameter,
            Type propertyType,
            MethodInfo isNullOrWhiteSpaceMethodInfo)
        {
            ConditionalExpression conditional =
                Expression.Condition(
                    test: Expression.Call(isNullOrWhiteSpaceMethodInfo, valueParameter),
                    ifTrue: Expression.Convert(Expression.Constant(null), propertyType),
                    ifFalse: Expression.Convert(valueToBeSetExpression, propertyType));

            return conditional;
        }

        private static MethodInfo GetIsNullOrWhiteSpaceMethodInfo()
        {
            MethodInfo isNullOrWhiteSpaceMethodInfo =
                            typeof(string)
                                 .GetMethod(nameof(string.IsNullOrWhiteSpace),
                                  BindingFlags.Static | BindingFlags.Public,
                                  null,
                                  new Type[] { typeof(string) },
                                  null);

            return isNullOrWhiteSpaceMethodInfo;
        }

        private static Expression GetEnumParseExpression(ParameterExpression valueParameter, Type propertyType)
        {
            MethodInfo methodParse =
                            typeof(Enum)
                                .GetMethod(nameof(Enum.Parse),
                                           BindingFlags.Static | BindingFlags.Public,
                                           null,
                                           new Type[]
                                           {
                                               typeof(Type),
                                               typeof(string),
                                               typeof(bool)
                                           },
                                           null);

            var parsedValue = Expression.Call(methodParse,
                                         Expression.Constant(propertyType),
                                         valueParameter,
                                         Expression.Constant(true));

            return Expression.Convert(parsedValue, propertyType);
        }

        private static Expression GetParseExpression(Type type, ParameterExpression valueParameter)
        {
            MethodInfo methodParse =
                            typeof(Convert)
                                .GetMethod(nameof(Convert.ChangeType),
                                           BindingFlags.Static | BindingFlags.Public,
                                           null,
                                           new Type[]
                                           {
                                               typeof(object),
                                               typeof(Type),
                                               typeof(CultureInfo)
                                           },
                                           null);

            var parsedValue = Expression.Call(methodParse,
                                         valueParameter,
                                         Expression.Constant(type),
                                         Expression.Constant(CultureInfo.InvariantCulture));

            return Expression.Convert(parsedValue, type);
        }
    }


    internal static class CreateInstanceHelper
    {
        public static Func<T> GetInstanceGenerator<T>(IEnumerable<string> mapped)
        {
            var root = new Node(typeof(T));

            foreach (var path in mapped)
                if (path != null)
                    root.AddPath(path);

            var newTObject = GetNewExpressionWithNestedMemberInit(root);

            var getNewInstance = Expression.Lambda<Func<T>>(newTObject).Compile();

            return getNewInstance;
        }

        private static MemberInitExpression GetNewExpressionWithNestedMemberInit(Node root)
        {
            var memberBinds = root
                ._nodes
                .Select(info =>
                    Expression.Bind(info.Value.Prop,
                                    GetNewExpressionWithNestedMemberInit(info.Value)));

            var newExpression = GetNewExpressionFor(root.Path);

            var member = Expression.MemberInit(newExpression, memberBinds);

            return member;
        }

        private static NewExpression GetNewExpressionFor(Type objType)
        {
            ConstructorInfo ctor = objType
                .GetConstructors()
                .OrderBy(x => x.GetParameters().Length)
                .First();

            return
                Expression.New
                (
                    ctor,
                    ctor.GetParameters().Select
                    (
                        x =>
                            x.IsOptional
                                ? Expression.Convert(Expression.Constant(x.DefaultValue), x.ParameterType)
                                : x.ParameterType.IsValueType && Nullable.GetUnderlyingType(x.ParameterType) == null
                                    ? Expression.Default(x.ParameterType)
                                    : (Expression)GetNewExpressionFor(x.ParameterType)
                    )
                );
        }

        internal class Node
        {
            public readonly IDictionary<string, Node> _nodes = new Dictionary<string, Node>();

            public Node(Type path) => Path = path;
            public Node(PropertyInfo prop) : this(prop.PropertyType)
            {
                Prop = prop;
            }

            public Type Path { get; private set; }
            public PropertyInfo Prop { get; private set; }

            public void AddPath(string path)
            {
                // Parse into a sequence of parts.
                string[] parts = path.Split('.',
                    StringSplitOptions.RemoveEmptyEntries);

                // The current node.  Start with this.
                Node current = this;

                // Iterate through the parts.
                foreach (string part in parts)
                {
                    // The child node.
                    Node child;

                    // Does the part exist in the current node?  If
                    // not, then add.
                    if (!current._nodes.TryGetValue(part, out child))
                    {
                        var prop = current.Path.GetProperty(part);
                        var childType = prop.PropertyType;

                        if (childType.IsValueType || childType == typeof(string))
                            return;

                        // Add the child.
                        child = new Node(prop);

                        // Add to the dictionary.
                        current._nodes[part] = child;
                    }

                    // Set the current to the child.
                    current = child;
                }
            }
        }
    }
}