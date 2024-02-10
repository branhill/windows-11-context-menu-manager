using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Windows11ContextMenuManager.Core;
using Windows11ContextMenuManager.Helpers;

namespace Windows11ContextMenuManager.ViewModels;

public partial class ItemViewModel : ObservableRecipient
{
    [ObservableProperty]
    private bool _isEnabled;

    public Extension Info { get; }

    private readonly Lazy<Task<Bitmap?>> _logo;
    public Task<Bitmap?> Logo => _logo.Value;

    [ObservableProperty]
    private bool _isExpanded;

    public ItemViewModel(Extension info)
    {
        Info = info;
        IsEnabled = GetIsEnabled();
        _logo = new Lazy<Task<Bitmap?>>(() => Task.Run(LoadLogo));
    }

    private Bitmap? LoadLogo()
    {
        try
        {
            return new Bitmap(Info.Package.Logo);
        }
        catch (Exception)
        {
            return null;
        }
    }

    [RelayCommand]
    private void UpdateIsEnabled()
    {
        Try.Run(() =>
        {
            if (IsEnabled)
                foreach (var blocks in Blocks.GetScopes())
                    blocks.Remove(Info.Id);
            else
                Blocks.GetScope(Blocks.WriteScope).Add(Info.Id);

            Messenger.Send(new Notification(
                "Success",
                $"{Info.Package.DisplayName} context menu {(IsEnabled ? "enabled" : "disabled")}",
                NotificationType.Success));
        });
        IsEnabled = GetIsEnabled();
    }

    [RelayCommand]
    private async Task CopyDisplayName(Visual sender)
    {
        await Copy(sender, Info.Package.DisplayName);
    }

    [RelayCommand]
    private async Task CopyFamilyName(Visual sender)
    {
        await Copy(sender, Info.Package.FamilyName);
    }

    [RelayCommand]
    private void OpenFileLocation()
    {
        Start(Info.Package.InstallPath);
    }

    [RelayCommand]
    private void AppSettings()
    {
        Start("ms-settings:appsfeatures-app");
    }

    [RelayCommand]
    private void MicrosoftStore()
    {
        Start($"ms-windows-store://pdp?PFN={Info.Package.FamilyName}");
    }

    [RelayCommand]
    private async Task Uninstall()
    {
        await Try.Run(async () =>
        {
            var info = new ProcessStartInfo
            {
                FileName = "powershell",
                UseShellExecute = true
            };
            info.ArgumentList.Add("-c");
            info.ArgumentList.Add($"Remove-AppxPackage {Info.Package.FullName} -Confirm");
            using var process = Process.Start(info);
            await (process?.WaitForExitAsync() ?? Task.CompletedTask);
            Messenger.Send(new ReloadMessage());
        });
    }

    private bool GetIsEnabled()
    {
        return !Blocks.GetScopes().Any(x => x.Contains(Info.Id));
    }

    private async Task Copy(Visual sender, string text)
    {
        if (TopLevel.GetTopLevel(sender)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(text);
            Messenger.Send(
                new Notification("Success", "Copied to clipboard", NotificationType.Success));
        }
    }

    private static void Start(string cmd)
    {
        Try.Run(() =>
        {
            using var _ = Process.Start(new ProcessStartInfo { FileName = cmd, UseShellExecute = true });
        });
    }
}