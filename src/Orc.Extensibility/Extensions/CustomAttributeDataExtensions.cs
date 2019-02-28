// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CustomAttributeDataExtensions.cs" company="WildGums">
//   Copyright (c) 2012 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Orc.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Catel;

    public static class CustomAttributeDataExtensions
    {
        public static object GetReflectionOnlyAttributeValue<TAttribute>(this IEnumerable<CustomAttributeData> customAttributes)
            where TAttribute : Attribute
        {
            var attribute = FilterCustomAttributes<TAttribute>(customAttributes).FirstOrDefault();
            if (attribute != null)
            {
                return attribute.ConstructorArguments[0].Value;
            }

            return null;
        }

        public static List<object> GetReflectionOnlyAttributeValues<TAttribute>(this IEnumerable<CustomAttributeData> customAttributes)
            where TAttribute : Attribute
        {
            var values = new List<object>();

            foreach (var attribute in FilterCustomAttributes<TAttribute>(customAttributes))
            {
                var value = attribute.ConstructorArguments[0].Value;
                if (value != null)
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
#if NETFX_CORE
                              let declaringTypeName = customAttributeData.AttributeType.Name
#else
                              let declaringTypeName = customAttributeData.Constructor.DeclaringType.Name
#endif
                              where declaringTypeName.EqualsIgnoreCase(typeof(TAttribute).Name)
                              select customAttributeData).ToList();

            return attributes;
        }
    }
}
