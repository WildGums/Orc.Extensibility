namespace Orc.Extensibility.Example.Watchers;

using Catel.IoC;
using Orc.Notifications;

internal class PluginBWatcher : IInitializeAtStartup
{
    private readonly INotificationService _notificationService;

    public PluginBWatcher(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public void Initialize()
    {
        _notificationService.ShowNotification(new Notification
        {
            Title = "Plugin B is loaded",
            Priority = NotificationPriority.High // required otherwise it will not show when there is no active window
        });
    }
}
