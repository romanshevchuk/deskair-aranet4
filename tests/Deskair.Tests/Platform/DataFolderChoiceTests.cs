using System.IO;
using Deskair.App.Platform;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Platform;

public sealed class DataFolderChoiceTests : IDisposable
{
    private readonly TempKnownFolders temp = new();
    private readonly string file;

    public DataFolderChoiceTests()
    {
        file = Path.Combine(temp.DataFolder, "pointer", "data-folder.txt");
    }

    public void Dispose()
    {
        temp.Dispose();
    }

    [Fact]
    public void Without_a_file_nothing_is_chosen()
    {
        Assert.Null(new DataFolderChoice(file).Chosen);
    }

    [Fact]
    public void A_choice_is_remembered_by_the_next_start()
    {
        var target = Path.Combine(temp.DataFolder, "my data");
        new DataFolderChoice(file).Choose(target);

        Assert.Equal(target, new DataFolderChoice(file).Chosen);
    }

    [Fact]
    public void Choosing_creates_the_chosen_folder_and_the_folder_of_the_file()
    {
        var target = Path.Combine(temp.DataFolder, "my data");

        new DataFolderChoice(file).Choose(target);

        Assert.True(Directory.Exists(target));
        Assert.True(File.Exists(file));
    }

    [Fact]
    public void A_blank_file_means_no_choice()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "  \r\n");

        Assert.Null(new DataFolderChoice(file).Chosen);
    }

    [Fact]
    public void A_file_that_cannot_be_read_means_no_choice()
    {
        Directory.CreateDirectory(file);

        Assert.Null(new DataFolderChoice(file).Chosen);
    }
}
