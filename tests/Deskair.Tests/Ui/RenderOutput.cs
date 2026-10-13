using System.IO;
using System.Runtime.CompilerServices;

namespace Deskair.Tests.Ui;

/// <summary>
/// The opt-in image output of the render tests. Setting <c>DESKAIR_RENDER=1</c> makes them write PNGs to <c>artifacts/visual</c> for a person to look at; without it they are skipped
/// (they assert nothing, so there is nothing to check automatically).
/// </summary>
public static class RenderOutput
{
    public static void Require()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("DESKAIR_RENDER") == "1", "Set DESKAIR_RENDER=1 to write the images to artifacts/visual.");
    }

    public static void Save(string fileName, byte[] png)
    {
        var folder = Folder();
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, fileName), png);
    }

    private static string Folder([CallerFilePath] string thisFile = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "artifacts", "visual"));
}
