namespace Orc.Extensibility
{
    public interface IPluginConfiguration
    {
        string PackagesDirectory { get; set; }

        string FeedUrl { get; set; }
        string FeedName { get; set; }
    }
}
