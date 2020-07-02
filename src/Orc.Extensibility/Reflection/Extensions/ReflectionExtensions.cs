namespace Orc.Extensibility
{
    using System;
    using System.Linq;
    using Catel;
    using Catel.Reflection;

    public static class ReflectionExtensions
    {
        public static bool ImplementsInterface<TInterface>(this Type type)
        {
            Argument.IsNotNull(() => type);

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
}
