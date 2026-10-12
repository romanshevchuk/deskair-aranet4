using System.Globalization;

namespace Deskair.App.Platform.Bluetooth;

/// <summary>The text form of a Bluetooth address that the app stores and shows: 12 upper-case hex digits without separators, for example <c>C3DEC8FCC7C6</c>.</summary>
public static class BleAddress
{
    public static string Format(ulong address) => (address & 0xFFFFFFFFFFFF).ToString("X12", CultureInfo.InvariantCulture);

    /// <summary>Accepts 12 hex digits, with or without ':' or '-' between the bytes, in any case.</summary>
    public static bool TryParse(string? text, out ulong address)
    {
        address = 0;

        if (text is null)
        {
            return false;
        }

        var digits = text.Replace(":", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);

        return digits.Length == 12 && ulong.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out address);
    }
}
