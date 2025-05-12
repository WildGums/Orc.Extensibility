namespace Orc.Extensibility
{
    public class PluginProbingLocation
    {
        public PluginProbingLocation()
        {
            IsRecursive = true;
        }

        public required string Location { get; set; }

        public bool IsRecursive { get; set; }

        public override string ToString()
        {
            return $"{Location} (Recursive: {IsRecursive})";
        }
    }
}
