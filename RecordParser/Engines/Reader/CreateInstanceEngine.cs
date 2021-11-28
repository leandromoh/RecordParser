using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RecordParser.Engines.Reader
{
    internal static class CreateInstanceEngine
    {
        public static Expression<Func<T>> GetInstanceGenerator<T>(IEnumerable<MemberExpression> mapped)
        {
            var root = new Node(typeof(T));

            foreach (var path in mapped)
                if (path != null)
                    root.AddPath(path);

            var newTObject = GetNewExpressionWithNestedMemberInit(root);

            var getNewInstance = Expression.Lambda<Func<T>>(newTObject);

            return getNewInstance;
        }

        private static MemberInitExpression GetNewExpressionWithNestedMemberInit(Node type)
        {
            var memberBinds = type
                .PropertiesToInitialize
                .Select(info =>
                    Expression.Bind(info.Value.MemberInfo.Member,
                                    GetNewExpressionWithNestedMemberInit(info.Value)));

            var newExpression = GetNewExpressionFor(type.MemberType);

            var member = Expression.MemberInit(newExpression, memberBinds);

            return member;
        }

        private static NewExpression GetNewExpressionFor(Type objType)
        {
            if (objType.IsValueType && Nullable.GetUnderlyingType(objType) == null)
                return Expression.New(objType);

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
                                ? Expression.Constant(x.DefaultValue, x.ParameterType)
                                : (Expression)GetNewExpressionFor(x.ParameterType)
                    )
                );
        }

        internal class Node
        {
            public readonly Dictionary<string, Node> PropertiesToInitialize = new ();

            public Node(Type path) => MemberType = path;
            public Node(MemberExpression prop) : this(prop.Type)
            {
                MemberInfo = prop;
            }

            public Type MemberType { get; private set; }
            public MemberExpression MemberInfo { get; private set; }

            public void AddPath(MemberExpression path)
            {
                var parts = path.GetNested();

                Node current = this;

                foreach (var part in parts)
                {
                    if (current.PropertiesToInitialize.TryGetValue(part.Member.Name, out Node child) is false)
                    {
                        var childType = part.Type;//.GetUnderlyingType();

                        if (childType.IsValueType || childType == typeof(string))
                            return;

                        current.PropertiesToInitialize[part.Member.Name] = child = new Node(part);
                    }

                    current = child;
                }
            }
        }
    }

    internal static class MemberExtensions
    {
        public static IEnumerable<MemberExpression> GetNested(this MemberExpression ex)
        {
            var q = new Stack<MemberExpression>();

            while (ex?.Expression != null)
            {
                q.Push(ex);
                ex = ex.Expression as MemberExpression;
            }

            return q;
        }
    }
}
