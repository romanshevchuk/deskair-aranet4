using Deskair.Domain.Readings;
using Deskair.Storage.Readings;
using Deskair.Tests.Fakes;

namespace Deskair.Tests.Storage;

public class ReadingIndexTests
{
    private static readonly DateTimeOffset noon = Samples.Utc(2026, 10, 9, 12);

    [Fact(Skip = "TODO(STORE-02a)")]
    public void Add_keeps_readings_sorted_by_time()
    {
        var index = new ReadingIndex();

        index.Add([Samples.Co2(noon.AddMinutes(4), 3), Samples.Co2(noon, 1), Samples.Co2(noon.AddMinutes(2), 2)]);

        var all = index.Range(noon, noon.AddHours(1));

        Assert.Equal(new[] { 1.0, 2.0, 3.0 }, all.Select(reading => reading.Co2Ppm));
    }

    [Fact(Skip = "TODO(STORE-02a)")]
    public void Add_keeps_the_existing_reading_for_a_duplicate_timestamp()
    {
        var index = new ReadingIndex();
        index.Add([Samples.Co2(noon, 1)]);

        index.Add([Samples.Co2(noon, 99)]);

        Assert.Equal(1, index.Range(noon, noon.AddMinutes(1)).Single().Co2Ppm);
    }

    [Fact(Skip = "TODO(STORE-02b)")]
    public void Range_is_start_inclusive_and_end_exclusive()
    {
        var index = new ReadingIndex();
        index.Add([Samples.Co2(noon, 1), Samples.Co2(noon.AddMinutes(2), 2), Samples.Co2(noon.AddMinutes(4), 3)]);

        var range = index.Range(noon, noon.AddMinutes(4));

        Assert.Equal(new[] { 1.0, 2.0 }, range.Select(reading => reading.Co2Ppm));
    }

    [Fact(Skip = "TODO(STORE-02b)")]
    public void Range_of_an_empty_or_reversed_span_is_empty()
    {
        var index = new ReadingIndex();
        index.Add([Samples.Co2(noon, 1)]);

        Assert.Empty(index.Range(noon, noon));
        Assert.Empty(index.Range(noon.AddHours(1), noon));
    }

    [Fact(Skip = "TODO(STORE-02c)")]
    public void Last_is_the_newest_timestamp_or_null()
    {
        var index = new ReadingIndex();

        Assert.Null(index.Last());

        index.Add([Samples.Co2(noon, 1), Samples.Co2(noon.AddMinutes(2), 2)]);

        Assert.Equal(noon.AddMinutes(2), index.Last());
    }
}
