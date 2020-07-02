namespace Orc.Extensibility
{
    using System;
    using System.Reflection.Metadata;
    using System.Text.RegularExpressions;
    using Catel;

    public static class ReflectionMetadataExtensions
    {
        private static readonly Regex StringCleanupRegex = new Regex(@"[^\u0009\u000A\u000D\u0020-\u007E]", RegexOptions.Compiled);

        public static string GetFullTypeName(this Type type)
        {
            var fullName = $"{type.Namespace}.{type.Name}";
            return fullName;
        }

        public static object GetCustomAttributeValue<TAttribute>(this CustomAttributeHandleCollection attributeHandles, MetadataReader reader)
        {
            var expectedAttributeFullName = typeof(TAttribute).GetFullTypeName();

            foreach (var customAttributeHandle in attributeHandles)
            {
                var customAttribute = reader.GetCustomAttribute(customAttributeHandle);

                var constructorHandle = customAttribute.Constructor;
                var constructor = reader.GetMemberReference((MemberReferenceHandle)constructorHandle);

                var customAttributeType = reader.GetTypeReference((TypeReferenceHandle)constructor.Parent);
                var customAttributeTypeName = GetFullTypeName(customAttributeType, reader);
                if (customAttributeTypeName.Equals(expectedAttributeFullName))
                {
                    var blobHandle = customAttribute.Value;
                    if (blobHandle.IsNil)
                    {
                        continue;
                    }

                    var blobReader = reader.GetBlobReader(blobHandle);

                    // For now just support strings
                    var value = blobReader.ReadUTF8(blobReader.RemainingBytes);

                    // Remove special characters
                    value = StringCleanupRegex.Replace(value, string.Empty);

                    if (value.StartsWithAny("$", "\r") && value.Length > 1)
                    {
                        value = value.Substring(1);
                    }

                    return value;
                }
            }

            return null;
        }

        public static bool ImplementsInterface<TInterface>(this TypeDefinition typeDefinition, MetadataReader reader)
        {
            return ImplementsInterface<TInterface>(typeDefinition.GetInterfaceImplementations(), reader);
        }

        public static bool ImplementsInterface<TInterface>(this InterfaceImplementationHandleCollection interfaceImplementations, MetadataReader reader)
        {
            var expectedInterfaceTypeName = typeof(TInterface).GetFullTypeName();

            foreach (var interfaceHandle in interfaceImplementations)
            {
                try
                {
                    var interfaceImplementation = reader.GetInterfaceImplementation(interfaceHandle);
                    var fullTypeName = string.Empty;

                    switch (interfaceImplementation.Interface.Kind)
                    {
                        case HandleKind.TypeDefinition:
                            var interfaceTypeDefinition = reader.GetTypeDefinition((TypeDefinitionHandle)interfaceImplementation.Interface);
                            fullTypeName = interfaceTypeDefinition.GetFullTypeName(reader);
                            break;

                        case HandleKind.TypeReference:
                            var interfaceTypeReference = reader.GetTypeReference((TypeReferenceHandle)interfaceImplementation.Interface);
                            fullTypeName = interfaceTypeReference.GetFullTypeName(reader);
                            break;
                    }

                    if (fullTypeName.Equals(expectedInterfaceTypeName))
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    // Ignore
                }
            }

            return false;
        }

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
