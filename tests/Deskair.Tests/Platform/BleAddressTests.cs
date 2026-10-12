using Deskair.App.Platform.Bluetooth;

namespace Deskair.Tests.Platform;

public class BleAddressTests
{
    [Fact]
    public void An_address_is_twelve_upper_case_hex_digits()
    {
        Assert.Equal("C3DEC8FCC7C6", BleAddress.Format(0xC3DEC8FCC7C6));
        Assert.Equal("00000000000A", BleAddress.Format(10));
    }

    [Theory]
    [InlineData("C3DEC8FCC7C6")]
    [InlineData("c3dec8fcc7c6")]
    [InlineData("C3:DE:C8:FC:C7:C6")]
    [InlineData("c3-de-c8-fc-c7-c6")]
    public void The_usual_spellings_are_accepted(string text)
    {
        Assert.True(BleAddress.TryParse(text, out var address));
        Assert.Equal(0xC3DEC8FCC7C6UL, address);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("SIM-0001")]
    [InlineData("C3DEC8FCC7")]
    [InlineData("C3DEC8FCC7C6C6")]
    [InlineData("C3DEC8FCC7CG")]
    public void Anything_else_is_rejected(string? text)
    {
        Assert.False(BleAddress.TryParse(text, out _));
    }

    [Fact]
    public void Parsing_undoes_formatting()
    {
        Assert.True(BleAddress.TryParse(BleAddress.Format(0x0123456789AB), out var address));
        Assert.Equal(0x0123456789ABUL, address);
    }
}
