using Deskair.App.Composition;
using Deskair.App.Platform;
using Deskair.Tests.Fakes;

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
        var folders = new KnownFolders(new AppOptions(false, Simulated: true), new FakeDataFolderChoice());
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        Assert.StartsWith(System.IO.Path.GetTempPath(), folders.DataFolder);
        Assert.DoesNotContain(documents, folders.DataFolder);
    }

    [Fact]
    public void A_single_sensor_test_run_keeps_its_data_in_the_temp_folder_never_in_documents()
    {
        var folders = new KnownFolders(new AppOptions(false, Sensor: "C3DEC8FCC7C6"), new FakeDataFolderChoice());
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        Assert.StartsWith(System.IO.Path.GetTempPath(), folders.DataFolder);
        Assert.DoesNotContain(documents, folders.DataFolder);
    }

    [Fact]
    public void The_sensor_option_is_read_with_its_value()
    {
        Assert.Equal("C3DEC8FCC7C6", AppOptions.Parse(["--sensor", "C3DEC8FCC7C6"]).Sensor);
        Assert.Null(AppOptions.Parse(["--sensor"]).Sensor);
    }

    [Fact]
    public void The_first_run_option_is_read_and_uses_its_own_temp_folder_never_documents()
    {
        Assert.True(AppOptions.Parse(["--first-run"]).FirstRun);
        Assert.False(AppOptions.Parse([]).FirstRun);

        var folders = new KnownFolders(new AppOptions(false, FirstRun: true), new FakeDataFolderChoice());
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        Assert.Equal(KnownFolders.FirstRunFolder, folders.DataFolder);
        Assert.StartsWith(System.IO.Path.GetTempPath(), folders.DataFolder);
        Assert.DoesNotContain(documents, folders.DataFolder);
    }

    [Fact]
    public void Resetting_the_first_run_folder_deletes_only_that_folder()
    {
        var inside = System.IO.Path.Combine(KnownFolders.FirstRunFolder, "devices");
        System.IO.Directory.CreateDirectory(inside);
        System.IO.File.WriteAllText(System.IO.Path.Combine(inside, "x.txt"), "x");
        var neighbour = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Deskair-first-run-neighbour-test");
        System.IO.Directory.CreateDirectory(neighbour);

        try
        {
            KnownFolders.ResetFirstRunFolder();

            Assert.False(System.IO.Directory.Exists(KnownFolders.FirstRunFolder));
            Assert.True(System.IO.Directory.Exists(neighbour));
        }
        finally
        {
            System.IO.Directory.Delete(neighbour, recursive: true);
        }
    }

    [Fact]
    public void The_real_app_keeps_its_data_in_documents()
    {
        var folders = new KnownFolders(new AppOptions(false), new FakeDataFolderChoice());

        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        Assert.Equal(System.IO.Path.Combine(documents, "Deskair"), folders.DataFolder);
    }

    [Fact]
    public void The_real_app_uses_the_folder_the_user_chose()
    {
        var folders = new KnownFolders(new AppOptions(false), new FakeDataFolderChoice { Chosen = @"D:\Air data" });

        Assert.Equal(@"D:\Air data", folders.DataFolder);
    }

    [Fact]
    public void Developer_runs_ignore_the_chosen_folder()
    {
        var choice = new FakeDataFolderChoice { Chosen = @"D:\Air data" };

        Assert.NotEqual(choice.Chosen, new KnownFolders(new AppOptions(false, Simulated: true), choice).DataFolder);
        Assert.NotEqual(choice.Chosen, new KnownFolders(new AppOptions(false, FirstRun: true), choice).DataFolder);
    }

    [Fact]
    public void Logs_stay_outside_the_data_folder()
    {
        var folders = new KnownFolders(new AppOptions(false), new FakeDataFolderChoice());

        Assert.DoesNotContain(folders.DataFolder, folders.LogFolder);
    }
}
