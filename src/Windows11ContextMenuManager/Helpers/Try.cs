using System.Security;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.Messaging;

namespace Windows11ContextMenuManager.Helpers;

public static class Try
{
    public static void Run(Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            Handle(e);
        }
    }

    public static async Task Run(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception e)
        {
            Handle(e);
        }
    }

    public static void Handle(Exception e)
    {
        string msg;
        switch (e)
        {
            case SecurityException:
            case UnauthorizedAccessException:
                msg = "Access denied, please try run as administrator.";
                break;
            default:
                msg = e.Message;
                break;
        }
        WeakReferenceMessenger.Default.Send(new Notification("Error", msg, NotificationType.Error));
    }
}