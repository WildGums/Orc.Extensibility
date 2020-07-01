namespace Orc.Extensibility
{
    using System.Reflection.Metadata;

    public static class ReflectionMetadataExtensions
    {
        public static string GetFullTypeName(this TypeDefinition typeDefinition, MetadataReader reader)
        {
            var ns = typeDefinition.Namespace.GetString(reader);
            var separator = string.IsNullOrWhiteSpace(ns) ? string.Empty : ".";
            var typeName = typeDefinition.Name.GetString(reader);

            return $"{ns}{separator}{typeName}";
        }

        public static string GetFullTypeName(this TypeReference typeReference, MetadataReader reader)
        {
            var ns = typeReference.Namespace.GetString(reader);
            var separator = string.IsNullOrWhiteSpace(ns) ? string.Empty : ".";
            var typeName = typeReference.Name.GetString(reader);

            return $"{ns}{separator}{typeName}";
        }

        public static string GetString(this StringHandle stringHandle, MetadataReader reader)
        {
            if (stringHandle.IsNil)
            {
                return null;
            }

            return reader.GetString(stringHandle);
        }
    }
}
