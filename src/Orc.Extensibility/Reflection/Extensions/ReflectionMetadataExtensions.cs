namespace Orc.Extensibility
{
    using System.Reflection.Metadata;

    public static class ReflectionMetadataExtensions
    {
        public static string GetFullTypeName(this TypeDefinition typeDefinition, MetadataReader reader, bool includeAssemblyName = false)
        {
            var ns = typeDefinition.Namespace.GetString(reader);
            var separator = string.IsNullOrWhiteSpace(ns) ? string.Empty : ".";
            var typeName = typeDefinition.Name.GetString(reader);

            var fullTypeName = $"{ns}{separator}{typeName}";

            if (includeAssemblyName)
            {
                var assemblyName = GetFullAssemblyName(reader);
                if (!string.IsNullOrWhiteSpace(assemblyName))
                {
                    fullTypeName += $", {assemblyName}";
                }
            }

            return fullTypeName;
        }

        public static string GetFullTypeName(this TypeReference typeReference, MetadataReader reader, bool includeAssemblyName = false)
        {
            var ns = typeReference.Namespace.GetString(reader);
            var separator = string.IsNullOrWhiteSpace(ns) ? string.Empty : ".";
            var typeName = typeReference.Name.GetString(reader);

            var fullTypeName = $"{ns}{separator}{typeName}";

            if (includeAssemblyName)
            {
                var assemblyName = GetFullAssemblyName(reader);
                if (!string.IsNullOrWhiteSpace(assemblyName))
                {
                    fullTypeName += $", {assemblyName}";
                }
            }

            return fullTypeName;
        }

        public static string GetFullAssemblyName(this MetadataReader reader)
        {
            var definition = reader.GetAssemblyDefinition();
            return GetString(definition.Name, reader);
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
