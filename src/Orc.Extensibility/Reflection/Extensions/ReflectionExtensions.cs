namespace Orc.Extensibility;

using System;
using Catel.Reflection;

public static class ReflectionExtensions
{
    public static bool ImplementsInterface<TInterface>(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var typeName = typeof(TInterface).FullName;

        foreach (var iface in type.GetInterfacesEx())
        {
            try
            {
                if (iface.FullName?.Equals(typeName) ?? false)
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
}