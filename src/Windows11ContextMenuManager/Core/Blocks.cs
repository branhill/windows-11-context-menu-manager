using System.Collections;
using System.Security;
using Microsoft.Win32;

namespace Windows11ContextMenuManager.Core;

public class Blocks(RegistryKey baseKey) : IReadOnlyList<string>
{
    internal const string RegKey = @"Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked";

    public static Blocks CurrentUser { get; } = new(Registry.CurrentUser);

    public static Blocks LocalMachine { get; } = new(Registry.LocalMachine);

    private List<string>? _values;
    private List<string> Values
    {
        get => _values ??= Load();
        set => _values = value;
    }

    public int Count => Values.Count;

    public string this[int index] => Values[index];

    private readonly Lazy<bool> _isReadOnly = new(() =>
    {
        try
        {
            using var subKey = baseKey.OpenSubKey(RegKey, true) ??
                               baseKey.OpenSubKey(RegKey[..RegKey.LastIndexOf('\\')], true);
            return false;
        }
        catch (SecurityException)
        {
            return true;
        }
    });
    public bool IsReadOnly => _isReadOnly.Value;

    private List<string> Load()
    {
        using var subKey = baseKey.OpenSubKey(RegKey);
        return subKey?.GetValueNames().Select(FromRegName).ToList() ?? [];
    }

    public void Reload()
    {
        Values = Load();
    }

    public void Add(string id)
    {
        using var subKey = baseKey.OpenSubKey(RegKey, true) ?? baseKey.CreateSubKey(RegKey);
        subKey.SetValue(ToRegName(id), "");
        Values.Add(id);
    }

    public void Remove(string id)
    {
        using var subKey = baseKey.OpenSubKey(RegKey, true);
        if (subKey is null)
        {
            Values = [];
        }
        else
        {
            subKey.DeleteValue(ToRegName(id), false);
            Values.Remove(id);
        }
    }

    public IEnumerator<string> GetEnumerator() => Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static string ToRegName(string val) => '{' + val + '}';

    private static string FromRegName(string val) => val.Trim('{', '}');
}