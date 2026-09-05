using CloudCure.Application.Common;

namespace CloudCure.Application.Tests;

public class PhoneNumberFormatterTests
{
    private readonly PhoneNumberFormatter _formatter = new();

    // (415) 555-2671 is the standard valid-format test number used throughout
    // libphonenumber's own test suite — real "555" *area codes* like 555-123-4567
    // aren't allocated NANP prefixes and correctly fail validity checks.
    [Theory]
    [InlineData("(415) 555-2671", "+14155552671")]
    [InlineData("415-555-2671", "+14155552671")]
    [InlineData("4155552671", "+14155552671")]
    [InlineData("+1 415 555 2671", "+14155552671")]
    public void ToE164_canonicalizes_various_input_formats_to_the_same_value(string input, string expected)
    {
        Assert.Equal(expected, _formatter.ToE164(input));
    }

    [Fact]
    public void ToE164_throws_for_an_unparseable_number()
    {
        Assert.Throws<FormatException>(() => _formatter.ToE164("not-a-phone-number"));
    }

    [Fact]
    public void ToDisplay_formats_a_canonical_US_number_for_presentation()
    {
        var display = _formatter.ToDisplay("+14155552671");

        Assert.Equal("(415) 555-2671", display);
    }

    [Fact]
    public void Round_trip_through_ToE164_then_ToDisplay_is_stable()
    {
        var e164 = _formatter.ToE164("415.555.2671");
        var display = _formatter.ToDisplay(e164);

        Assert.Equal("(415) 555-2671", display);
    }
}
