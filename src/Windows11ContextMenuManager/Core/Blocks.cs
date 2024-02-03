using System.Collections;
using System.Security;
using Microsoft.Win32;

namespace Windows11ContextMenuManager.Core;

public class Blocks : IReadOnlyCollection<string>
{
    internal const string RegKey = @"Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked";

    private readonly RegistryKey _baseKey;

    private HashSet<string> _items = [];

    private readonly Lazy<bool> _isReadOnly;
    public bool IsReadOnly => _isReadOnly.Value;

    public int Count => _items.Count;

    public BlockScope Scope { get; }

    private Blocks(BlockScope scope)
    {
        Scope = scope;
        _baseKey = RegistryKey.OpenBaseKey((RegistryHive)scope, RegistryView.Default);
        _isReadOnly = new(() =>
        {
            try
            {
                using var subKey = _baseKey.OpenSubKey(RegKey, true) ??
                                   _baseKey.OpenSubKey(RegKey[..RegKey.LastIndexOf('\\')], true);
                return false;
            }
            catch (SecurityException)
            {
                return true;
            }
        });
    }

    public void Load()
    {
        using var subKey = _baseKey.OpenSubKey(RegKey);
        _items = subKey?.GetValueNames().Select(FromRegName).ToHashSet() ?? [];
    }

    public void Add(string id)
    {
        using var subKey = _baseKey.OpenSubKey(RegKey, true) ?? _baseKey.CreateSubKey(RegKey);
        subKey.SetValue(ToRegName(id), "");
        _items.Add(id);
    }

    public void Remove(string id)
    {
        if (!_items.Contains(id))
            return;
        using var subKey = _baseKey.OpenSubKey(RegKey, true);
        if (subKey is null)
        {
            _items = [];
        }
        else
        {
            subKey.DeleteValue(ToRegName(id), false);
            _items.Remove(id);
        }
    }

    public bool Contains(string id) => _items.Contains(id);

    public IEnumerator<string> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static string ToRegName(string val) => '{' + val + '}';

    private static string FromRegName(string val) => val.Trim('{', '}');

    public static Blocks User { get; } = new(BlockScope.User);

    public static Blocks Machine { get; } = new(BlockScope.Machine);

    public static BlockScope WriteScope { get; set; } = BlockScope.User;

    public static IEnumerable<Blocks> GetScopes()
    {
        yield return User;
        yield return Machine;
    }

    public static Blocks GetScope(BlockScope scope)
    {
        return scope switch
        {
            BlockScope.User => User,
            BlockScope.Machine => Machine,
            _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null)
        };
    }

    public static void LoadAll()
    {
        foreach (var blocks in GetScopes())
            blocks.Load();
    }
}

public enum BlockScope
{
    User = RegistryHive.CurrentUser,
    Machine = RegistryHive.LocalMachine
}
