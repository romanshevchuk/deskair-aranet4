using Deskair.App.Platform.Files;

namespace Deskair.Tests.Platform;

public class ExplorerFolderOpenerTests
{
    private const string LocalAppData = @"C:\Users\Ann\AppData\Local";
    private const string PackageCache = @"C:\Users\Ann\AppData\Local\Packages\Deskair_abc\LocalCache";

    [Fact]
    public void A_folder_under_local_app_data_is_found_in_the_package_copy()
    {
        var redirected = ExplorerFolderOpener.RedirectedFolder(@"C:\Users\Ann\AppData\Local\Deskair\logs", LocalAppData, PackageCache);

        Assert.Equal(@"C:\Users\Ann\AppData\Local\Packages\Deskair_abc\LocalCache\Local\Deskair\logs", redirected);
    }

    [Theory]
    [InlineData(@"C:\Users\Ann\Documents\Deskair")]
    [InlineData(@"D:\Air data")]
    [InlineData(@"C:\Users\Ann\AppData\Local")]
    [InlineData(@"C:\Users\Ann\AppData\LocalLow\Deskair")]
    public void A_folder_elsewhere_is_not_redirected(string folder)
    {
        Assert.Null(ExplorerFolderOpener.RedirectedFolder(folder, LocalAppData, PackageCache));
    }
}
