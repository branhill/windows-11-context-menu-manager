using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;

namespace Windows11ContextMenuManager.Views;

public partial class MainWindow : Window, IRecipient<Notification>
{
    private WindowNotificationManager? _notificationManager;

    public MainWindow()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.Register(this);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _notificationManager = new(this)
        {
            Classes = { "bottom-center" }
        };
    }

    public void Receive(Notification message)
    {
        Dispatcher.UIThread.Invoke(() => _notificationManager?.Show(message));
    }
}