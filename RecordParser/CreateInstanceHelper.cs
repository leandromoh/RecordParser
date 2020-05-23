using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RecordParser
{
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

        private static MemberInitExpression GetNewExpressionWithNestedMemberInit(Node type)
        {
            var memberBinds = type
                .PropertiesToInitialize
                .Select(info =>
                    Expression.Bind(info.Value.MemberInfo,
                                    GetNewExpressionWithNestedMemberInit(info.Value)));

            var newExpression = GetNewExpressionFor(type.MemberType);

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
            public readonly IDictionary<string, Node> PropertiesToInitialize = new Dictionary<string, Node>();

            public Node(Type path) => MemberType = path;
            public Node(MemberInfo prop) : this(prop.GetUnderlyingType())
            {
                MemberInfo = prop;
            }

            public Type MemberType { get; private set; }
            public MemberInfo MemberInfo { get; private set; }

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
                    if (!current.PropertiesToInitialize.TryGetValue(part, out child))
                    {
                        var prop = current.MemberType.GetMember(part)[0];
                        var childType = prop.GetUnderlyingType();

                        if (childType.IsValueType || childType == typeof(string))
                            return;

                        // Add the child.
                        child = new Node(prop);

                        // Add to the dictionary.
                        current.PropertiesToInitialize[part] = child;
                    }

                    // Set the current to the child.
                    current = child;
                }
            }
        }
    }

    public static class MemberExtensions
    {
        public static Type GetUnderlyingType(this MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Event:
                    return ((EventInfo)member).EventHandlerType;
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Method:
                    return ((MethodInfo)member).ReturnType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
                default:
                    throw new ArgumentException
                    (
                     "Input MemberInfo must be if type EventInfo, FieldInfo, MethodInfo, or PropertyInfo"
                    );
            }
        }
    }
}
