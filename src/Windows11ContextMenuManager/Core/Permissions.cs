using System.Security.Principal;

namespace Windows11ContextMenuManager.Core;

public static class Permissions
{
    private static readonly Lazy<bool> IsElevatedLazy = new(() =>
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    });

    public static bool IsElevated => IsElevatedLazy.Value;
}