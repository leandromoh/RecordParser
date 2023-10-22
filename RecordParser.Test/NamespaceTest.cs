using FluentAssertions;
using RecordParser.Extensions;
using System;
using System.Linq;
using Xunit;

namespace RecordParser.Test
{
    public class NamespaceTest : TestSetup
    {
        [Fact]
        public void Given_public_types_inside_extensions_folder_should_not_have_namespaces_referencing_subfolder()
        {
            // Arrange

            bool IsDelegate(Type type) => typeof(Delegate).IsAssignableFrom(type.BaseType);

            var location = "RecordParser.Extensions";
            var types = typeof(WriterExtensions)
                .Assembly
                .GetTypes()
                .Where(x => x.IsPublic && IsDelegate(x) is false)
                .Where(x => x.Namespace.StartsWith(location))
                .ToArray();

            // Act

            var typesDifferentLocation = types.Where(x => x.Namespace != location);

            // Assert

            typesDifferentLocation.Should().BeEmpty();
        }
    }
}
