using System.IO;
using Deskair.Services.Platform;

namespace Deskair.App.Platform;

/// <summary>Remembers the chosen data folder in a one-line text file. A missing, blank or unreadable file means no choice.</summary>
public sealed class DataFolderChoice : IDataFolderChoice
{
    private readonly string file;

    public DataFolderChoice(string file)
    {
        this.file = file;
        Chosen = Read();
    }

    public string? Chosen { get; private set; }

    public void Choose(string folder)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        Directory.CreateDirectory(folder);
        File.WriteAllText(file, folder);
        Chosen = folder;
    }

    private string? Read()
    {
        try
        {
            var text = File.Exists(file) ? File.ReadAllText(file).Trim() : string.Empty;

            return text.Length == 0 ? null : text;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
