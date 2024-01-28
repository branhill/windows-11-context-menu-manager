using Microsoft.Win32;
using Windows11ContextMenuManager.Core;

namespace Windows11ContextMenuManager.Tests;

public class BlocksTests
{
    [Fact]
    public void IsReadOnlyTest()
    {
        Assert.False(Blocks.CurrentUser.IsReadOnly);
        Assert.NotEqual(Permissions.IsElevated, Blocks.LocalMachine.IsReadOnly);
    }

    [Fact]
    public void ReadWriteTest()
    {
        var blocks = Blocks.CurrentUser;
        var id = Guid.NewGuid().ToString().ToUpper();
        var beforeCount = blocks.Count;

        blocks.Add(id);

        Assert.Equal(beforeCount + 1, blocks.Count);
        Assert.Contains(id, blocks);

        blocks.Reload();

        Assert.Equal(beforeCount + 1, blocks.Count);
        Assert.Contains(id, blocks);

        using (var subKey = Registry.CurrentUser.OpenSubKey(Blocks.RegKey))
        {
            Assert.NotNull(subKey);
            Assert.NotNull(subKey.GetValue('{' + id + '}'));
        }

        blocks.Remove(id);

        Assert.Equal(beforeCount, blocks.Count);
        Assert.DoesNotContain(id, blocks);

        blocks.Reload();

        Assert.Equal(beforeCount, blocks.Count);
        Assert.DoesNotContain(id, blocks);
    }
}