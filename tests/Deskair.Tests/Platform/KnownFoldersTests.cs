using System.IO;
using Deskair.App.Composition;
using Deskair.App.Platform;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Platform;

public class KnownFoldersTests
{
    private static readonly string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    public static TheoryData<AppOptions> DeveloperRuns => new()
    {
        new AppOptions(false, Simulated: true),
        new AppOptions(false, Sensor: "C3DEC8FCC7C6"),
        new AppOptions(false, FirstRun: true)
    };

    [Fact]
    public void The_real_app_keeps_its_data_in_documents()
    {
        var folders = new KnownFolders(new AppOptions(false), new FakeDataFolderChoice());

        Assert.Equal(Path.Combine(documents, "Deskair"), folders.DataFolder);
    }

    [Fact]
    public void The_real_app_uses_the_folder_the_user_chose()
    {
        var folders = new KnownFolders(new AppOptions(false), new FakeDataFolderChoice { Chosen = @"D:\Air data" });

        Assert.Equal(@"D:\Air data", folders.DataFolder);
    }

    [Theory]
    [MemberData(nameof(DeveloperRuns))]
    public void Developer_runs_keep_their_data_in_the_temp_folder_never_in_documents(AppOptions options)
    {
        var folders = new KnownFolders(options, new FakeDataFolderChoice());

        Assert.StartsWith(Path.GetTempPath(), folders.DataFolder);
        Assert.DoesNotContain(documents, folders.DataFolder);
    }

    [Theory]
    [MemberData(nameof(DeveloperRuns))]
    public void Developer_runs_ignore_the_chosen_folder(AppOptions options)
    {
        var choice = new FakeDataFolderChoice { Chosen = @"D:\Air data" };

        Assert.NotEqual(choice.Chosen, new KnownFolders(options, choice).DataFolder);
    }

    [Fact]
    public void The_first_run_option_uses_its_own_folder()
    {
        var folders = new KnownFolders(new AppOptions(false, FirstRun: true), new FakeDataFolderChoice());

        Assert.Equal(KnownFolders.FirstRunFolder, folders.DataFolder);
    }

    [Fact]
    public void Resetting_the_first_run_folder_deletes_only_that_folder()
    {
        var inside = Path.Combine(KnownFolders.FirstRunFolder, "devices");
        Directory.CreateDirectory(inside);
        File.WriteAllText(Path.Combine(inside, "x.txt"), "x");
        var neighbour = Path.Combine(Path.GetTempPath(), "Deskair-first-run-neighbour-test");
        Directory.CreateDirectory(neighbour);

        try
        {
            KnownFolders.ResetFirstRunFolder();

            Assert.False(Directory.Exists(KnownFolders.FirstRunFolder));
            Assert.True(Directory.Exists(neighbour));
        }
        finally
        {
            Directory.Delete(neighbour, recursive: true);
        }
    }

    [Fact]
    public void Logs_stay_outside_the_data_folder()
    {
        var folders = new KnownFolders(new AppOptions(false), new FakeDataFolderChoice());

        Assert.DoesNotContain(folders.DataFolder, folders.LogFolder);
    }
}
