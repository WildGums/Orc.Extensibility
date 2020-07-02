namespace Orc.Extensibility
{
    using System;
    using System.Linq;
    using Catel.Reflection;

    public static class ReflectionExtensions
    {
        public static bool ImplementsInterface<TInterface>(this Type type)
        {
            var typeName = typeof(TInterface).FullName;

            return (from iface in type.GetInterfacesEx()
                    where iface.FullName.Equals(typeName)
                    select iface).Any();
        }
    }
}
