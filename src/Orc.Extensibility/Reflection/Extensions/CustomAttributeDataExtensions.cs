namespace Orc.Extensibility;

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
            return GetAttributeValue(attribute);
        }

        return null;
    }

    public static IReadOnlyList<object> GetAttributeValues<TAttribute>(this IEnumerable<CustomAttributeData> customAttributes)
        where TAttribute : Attribute
    {
        var values = new List<object>();

        foreach (var attribute in FilterCustomAttributes<TAttribute>(customAttributes))
        {
            var value = GetAttributeValue(attribute);
            if (value is not null)
            {
                values.Add(value);
            }
        }

        return values;
    }

    private static object? GetAttributeValue(this CustomAttributeData customAttributeData)
    {
        if (customAttributeData.ConstructorArguments.Count > 0)
        {
            return customAttributeData.ConstructorArguments[0].Value;
        }

        return null;    
    }

    private static IReadOnlyList<CustomAttributeData> FilterCustomAttributes<TAttribute>(this IEnumerable<CustomAttributeData> customAttributes)
        where TAttribute : Attribute
    {
        var attributes = new List<CustomAttributeData>();

        foreach (var customAttributeData in customAttributes)
        {
            try
            {
                var declaringTypeName = customAttributeData.Constructor.DeclaringType?.Name;
                if (string.IsNullOrEmpty(declaringTypeName))
                {
                    continue;
                }
                
                if (!declaringTypeName.EqualsIgnoreCase(typeof(TAttribute).Name))
                {
                    continue;
                }

                attributes.Add(customAttributeData);
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        return attributes;
    }
}
