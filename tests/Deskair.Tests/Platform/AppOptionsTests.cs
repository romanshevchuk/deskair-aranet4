using Deskair.App.Composition;
using Deskair.App.Platform;

namespace Deskair.Tests.Platform;

public class AppOptionsTests
{
    [Fact]
    public void No_arguments_run_the_real_app()
    {
        var options = AppOptions.Parse([]);

        Assert.False(options.Gallery);
        Assert.False(options.Simulated);
    }

    [Theory]
    [InlineData("--gallery")]
    [InlineData("--GALLERY")]
    public void The_gallery_flag_is_found_in_any_case(string argument)
    {
        Assert.True(AppOptions.Parse([argument]).Gallery);
    }

    [Fact]
    public void The_simulated_flag_is_found_next_to_other_arguments()
    {
        var options = AppOptions.Parse(["--other", "--simulated"]);

        Assert.True(options.Simulated);
        Assert.False(options.Gallery);
    }

    [Fact]
    public void The_simulated_app_keeps_its_data_in_the_temp_folder_never_in_documents()
    {
        var folders = new KnownFolders(new AppOptions(false, Simulated: true));
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        Assert.StartsWith(System.IO.Path.GetTempPath(), folders.DataFolder);
        Assert.DoesNotContain(documents, folders.DataFolder);
    }

    [Fact]
    public void The_real_app_keeps_its_data_in_documents()
    {
        var folders = new KnownFolders(new AppOptions(false));

        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        Assert.Equal(System.IO.Path.Combine(documents, "Deskair"), folders.DataFolder);
    }

    [Fact]
    public void Logs_stay_outside_the_data_folder()
    {
        var folders = new KnownFolders(new AppOptions(false));

        Assert.DoesNotContain(folders.DataFolder, folders.LogFolder);
    }
}
