﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RecordParser.Generic
{
    internal static class CreateInstanceHelper
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
                                ? Expression.Convert(Expression.Constant(x.DefaultValue), x.ParameterType)
                                : (Expression)GetNewExpressionFor(x.ParameterType)
                    )
                );
        }

        internal class Node
        {
            public readonly IDictionary<string, Node> PropertiesToInitialize = new Dictionary<string, Node>();

            public Node(Type path) => MemberType = path;
            public Node(MemberExpression prop) : this(prop.Type)
            {
                MemberInfo = prop;
            }

            public Type MemberType { get; private set; }
            public MemberExpression MemberInfo { get; private set; }

            public void AddPath(MemberExpression path)
            {
                // Parse into a sequence of parts.
                var parts = path.GetNested();

                // The current node.  Start with this.
                Node current = this;

                // Iterate through the parts.
                foreach (var part in parts)
                {
                    // The child node.
                    Node child;

                    // Does the part exist in the current node?  If
                    // not, then add.
                    if (!current.PropertiesToInitialize.TryGetValue(part.Member.Name, out child))
                    {
                        var childType = part.Type;//.GetUnderlyingType();

                        if (childType.IsValueType || childType == typeof(string))
                            return;

                        // Add the child.
                        child = new Node(part);

                        // Add to the dictionary.
                        current.PropertiesToInitialize[part.Member.Name] = child;
                    }

                    // Set the current to the child.
                    current = child;
                }
            }
        }
    }

    public static class MemberExtensions
    {
        public static IEnumerable<MemberExpression> GetNested(this MemberExpression ex)
        {
            var q = new List<MemberExpression>();

            while (ex?.Expression != null)
            {
                q.Add(ex);
                ex = ex.Expression as MemberExpression;
            }

            q.Reverse();

            return q;
        }
    }
}
