namespace Deskair.Tests.Fakes;

/// <summary>Keeps every value reported to it, in order, on the thread that reports (unlike <see cref="Progress{T}"/>, which posts).</summary>
public sealed class ListProgress<T> : IProgress<T>
{
    public List<T> Reports { get; } = [];

    public void Report(T value) => Reports.Add(value);
}
