using System.Collections.Concurrent;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Win32;
using Windows.Management.Deployment;
using Windows11ContextMenuManager.Helpers;

namespace Windows11ContextMenuManager.Core;

public static class Packages
{
    private const string NsCom = "http://schemas.microsoft.com/appx/manifest/com/windows10";
    private const string NsDesktop4 = "http://schemas.microsoft.com/appx/manifest/desktop/windows10/4";

    public static async Task<ICollection<Extension>> GetExtensions()
    {
        var comPackages = GetPackagedComPackages();
        var packageManager = new PackageManager();
        var extensions = new ConcurrentDictionary<string, Extension>();
        await Parallel.ForEachAsync(comPackages, new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (fullName, _) =>
        {
            try
            {
                var package = Permissions.IsElevated
                    ? packageManager.FindPackage(fullName)
                    : packageManager.FindPackageForUser("", fullName);
                if (package is null)
                    return;
                var ver = package.Id.Version;
                var pkg = new Pkg(
                    package.Id.FamilyName,
                    package.Id.FullName,
                    package.DisplayName,
                    package.PublisherDisplayName,
                    package.Logo.LocalPath,
                    package.InstalledLocation.Path,
                    new Version(ver.Major, ver.Minor, ver.Build, ver.Revision));
                var res = await AnalyzeManifest(pkg, package.IsBundle);
                foreach (var item in res)
                {
                    if (!extensions.TryGetValue(item.Id, out var existing) ||
                        existing.Package.Version < item.Package.Version)
                    {
                        extensions[item.Id] = item;
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        });
        return extensions.Values;
    }

    internal static string[] GetPackagedComPackages()
    {
        using var subKey = Registry.ClassesRoot.OpenSubKey(@"PackagedCom\Package");
        return subKey?.GetSubKeyNames() ?? [];
    }

    internal static async Task<IEnumerable<Extension>> AnalyzeManifest(Pkg pkg, bool isBundle)
    {
        var manifestPath = Path.Join(
            pkg.InstallPath,
            isBundle ? @"\AppxMetadata\AppxBundleManifest.xml" : @"\AppxManifest.xml");
        await using var stream = File.OpenRead(manifestPath);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            Async = true,
            DtdProcessing = DtdProcessing.Ignore
        });

        var nsResolver = (IXmlNamespaceResolver)reader;
        if (!reader.ReadToFollowing("Package") ||
            nsResolver.LookupPrefix(NsDesktop4) is null ||
            nsResolver.LookupPrefix(NsCom) is null)
        {
            return [];
        }

        var contextMenus = new Dictionary<string, List<ContextMenu>>();
        var comServers = new Dictionary<string, ComServer>();
        while (await reader.ReadAsync())
        {
            if (reader.NodeType != XmlNodeType.Element)
                continue;
            switch (reader.LocalName)
            {
                case "FileExplorerContextMenus":
                {
                    var el = (XElement)XNode.ReadFrom(reader);
                    var query =
                        from itemType in el.Elements()
                        where itemType.Name.LocalName == "ItemType"
                        from verb in itemType.Elements()
                        where verb.Name.LocalName == "Verb"
                        let type = itemType.Attribute("Type")?.Value
                        let item = new ContextMenu(
                            verb.Attribute("Clsid")?.Value,
                            verb.Attribute("Id")?.Value,
                            type.Contains("Directory") ? type : $"File: {type}")
                        group item by item.Clsid;
                    contextMenus = query.ToDictionary(x => x.Key, x => x.ToList());
                    break;
                }
                case "ComServer":
                {
                    var el = (XElement)XNode.ReadFrom(reader);
                    var query =
                        from server in el.Elements()
                        where server.Name.LocalName is "SurrogateServer" or "ExeServer"
                        from cls in server.Elements()
                        where cls.Name.LocalName == "Class"
                        let item = new ComServer(
                            cls.Attribute("Id")?.Value,
                            Path.Join(
                                pkg.InstallPath,
                                cls.Attribute("Path")?.Value ?? server.Attribute("Executable")?.Value),
                            server.Attribute("DisplayName")?.Value)
                        group item by item.Id;
                    comServers = query.ToDictionary(x => x.Key, x => x.First());
                    break;
                }
            }
        }

        return contextMenus
            .Select(x => x.Key)
            .Intersect(comServers.Select(x => x.Key))
            .Select(id => new Extension(id, pkg, contextMenus[id], comServers[id]));
    }
}

public record Pkg(
    string FamilyName,
    string FullName,
    string DisplayName,
    string PublisherDisplayName,
    string Logo,
    string InstallPath,
    Version Version);

public record ContextMenu(
    string? Clsid,
    string? Id,
    string? Type);

public record ComServer(
    string? Id,
    string? Path,
    string? DisplayName);

public record Extension(
    string Id,
    Pkg Package,
    List<ContextMenu> ContextMenus,
    ComServer ComServer);
