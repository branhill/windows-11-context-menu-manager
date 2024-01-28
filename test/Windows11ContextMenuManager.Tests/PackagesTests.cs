using Windows11ContextMenuManager.Core;

namespace Windows11ContextMenuManager.Tests;

public class PackagesTests
{
    [Fact]
    public async Task GetExtensionsTest()
    {
        var extensions = await Packages.GetExtensions();

        Assert.NotEmpty(extensions);
    }

    [Theory]
    [MemberData(nameof(ManifestData))]
    public async Task AnalyzeManifestTest(string name, ExtensionExpected[] expected)
    {
        var installPath = Path.Join("Data", name);
        var pkg = new Pkg(name, name, name, name, name, installPath);

        var res = (await Packages.AnalyzeManifest(pkg, false)).ToDictionary(x => x.Id, x => x);

        Assert.Equal(expected.Select(x => x.Id), res.Keys);
        foreach (var exp in expected)
        {
            var actual = res[exp.Id];
            Assert.Equal(Path.Join(installPath, exp.Path), actual.ComServer.Path);
            Assert.Equal(exp.Types, actual.ContextMenus.Select(x => x.Type));
        }
    }

    public static TheoryData<string, ExtensionExpected[]> ManifestData()
    {

        return new()
        {
            {
                "Microsoft.WindowsNotepad", [
                    new ExtensionExpected(
                        "CA6CC9F1-867A-481E-951E-A28C5E4F01EA",
                        @"NotepadExplorerCommand\NotepadExplorerCommand.dll",
                        ["*", "Directory"])
                ]
            }
        };
    }

    public record ExtensionExpected(
        string Id,
        string Path,
        string[] Types);
}