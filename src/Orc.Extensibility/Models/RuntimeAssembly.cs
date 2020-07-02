namespace Orc.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class RuntimeAssembly
    {
        public RuntimeAssembly(string name, string location, string source)
        {
            Name = name;
            Location = location;
            Source = source;
        }

        public string Name { get; private set; }

        public string Location { get; private set; }

        public string Source { get; private set; }
    }
}
