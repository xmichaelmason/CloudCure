using PhoneNumbers;

namespace CloudCure.Application.Common;

/// <summary>
/// Canonicalizes phone numbers to E.164 for storage and formats them for display —
/// replaces the old app's inconsistent stored-phone formatting (the root cause of its
/// recurring phone-mask bugs) by never storing anything but the canonical form.
/// </summary>
public class PhoneNumberFormatter(string defaultRegion = "US")
{
    private readonly PhoneNumberUtil _phoneNumberUtil = PhoneNumberUtil.GetInstance();

    public string ToE164(string rawInput)
    {
        try
        {
            var parsed = _phoneNumberUtil.Parse(rawInput, defaultRegion);
            if (!_phoneNumberUtil.IsValidNumber(parsed))
            {
                throw new FormatException($"'{rawInput}' is not a valid phone number.");
            }

            return _phoneNumberUtil.Format(parsed, PhoneNumberFormat.E164);
        }
        catch (NumberParseException ex)
        {
            throw new FormatException($"'{rawInput}' is not a valid phone number.", ex);
        }
    }

    public string ToDisplay(string e164)
    {
        var parsed = _phoneNumberUtil.Parse(e164, defaultRegion);
        return _phoneNumberUtil.Format(parsed, PhoneNumberFormat.NATIONAL);
    }
}
