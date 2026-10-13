using Deskair.Services.Platform;

namespace Deskair.Tests.Fakes;

public sealed class FakeDataFolderChoice : IDataFolderChoice
{
    public string? Chosen { get; set; }

    /// <summary>When set, <see cref="Choose"/> throws it instead of remembering the folder.</summary>
    public Exception? Failure { get; set; }

    public void Choose(string folder)
    {
        if (Failure is not null)
        {
            throw Failure;
        }

        Chosen = folder;
    }
}
