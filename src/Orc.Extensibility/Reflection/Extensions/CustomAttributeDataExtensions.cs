namespace Orc.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Catel;

    public static class CustomAttributeDataExtensions
    {
        public static object? GetAttributeValue<TAttribute>(this IEnumerable<CustomAttributeData> customAttributes)
            where TAttribute : Attribute
        {
            var attribute = FilterCustomAttributes<TAttribute>(customAttributes).FirstOrDefault();
            if (attribute is not null)
            {
                return attribute.ConstructorArguments[0].Value;
            }

            return null;
        }

        public static List<object> GetAttributeValues<TAttribute>(this IEnumerable<CustomAttributeData> customAttributes)
            where TAttribute : Attribute
        {
            var values = new List<object>();

            foreach (var attribute in FilterCustomAttributes<TAttribute>(customAttributes))
            {
                var value = attribute.ConstructorArguments[0].Value;
                if (value is not null)
                {
                    values.Add(value);
                }
            }

            return values;
        }

        private static List<CustomAttributeData> FilterCustomAttributes<TAttribute>(this IEnumerable<CustomAttributeData> customAttributes)
            where TAttribute : Attribute
        {
            var attributes = (from customAttributeData in customAttributes
                              let declaringTypeName = customAttributeData.Constructor.DeclaringType?.Name
                              where !string.IsNullOrEmpty(declaringTypeName) && declaringTypeName.EqualsIgnoreCase(typeof(TAttribute).Name)
                              select customAttributeData).ToList();

            return attributes;
        }
    }
}
