using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows11ContextMenuManager.Core;
using Windows11ContextMenuManager.Helpers;

namespace Windows11ContextMenuManager.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayItems))]
    private List<ItemViewModel> _items = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayItems))]
    private string? _search;

    [ObservableProperty]
    private BlockScope? _scope = Blocks.WriteScope;

    [ObservableProperty]
    private long _loadElapsed;

    public BlockScopeItem[] Scopes { get; } = Blocks.GetScopes()
        .Select(x => new BlockScopeItem(x.Scope, x.Scope.ToString(), !x.IsReadOnly))
        .ToArray();

    public IEnumerable<ItemViewModel> DisplayItems
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Search))
                return Items;
            var search = Search.Trim();
            return Items.Where(x =>
                Contains(x.Info.Package.DisplayName) ||
                Contains(x.Info.Package.FamilyName) ||
                Contains(x.Info.Package.InstallPath) ||
                Contains(x.Info.ComServer.Id) ||
                Contains(x.Info.ComServer.DisplayName) ||
                Contains(x.Info.ComServer.Path) ||
                x.Info.ContextMenus.Any(m =>
                    Contains(m.Id) ||
                    Contains(m.Type)));
            bool Contains(string? val) => val?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;
        }
    }

    public MainViewModel()
    {
        Task.Run(Load);
    }

    [RelayCommand]
    private async Task Load()
    {
        var stopwatch = Stopwatch.StartNew();

        await Try.Run(async () =>
        {
            Blocks.LoadAll();
            var extensions = await Packages.GetExtensions();
            Items = extensions
                .OrderBy(x => x.Package.DisplayName)
                .Select(x => new ItemViewModel(x))
                .ToList();
        });

        stopwatch.Stop();
        LoadElapsed = stopwatch.ElapsedMilliseconds;
    }

    [RelayCommand]
    private void ExpandAll()
    {
        foreach (var item in Items)
            item.IsExpanded = true;
    }

    [RelayCommand]
    private void CollapseAll()
    {
        foreach (var item in Items)
            item.IsExpanded = false;
    }

    partial void OnScopeChanged(BlockScope? value)
    {
        if (value is { } val)
            Blocks.WriteScope = val;
    }
}

public record BlockScopeItem(
    BlockScope Value,
    string Name,
    bool IsEnabled);