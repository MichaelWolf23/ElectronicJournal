using System.Linq;
using System.Net.Mail;

namespace ElectronicJournal.Utilities;

public static class InputValidator
{
    public static bool IsEmailValid(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        try
        {
            var address = new MailAddress(value.Trim());
            return address.Address == value.Trim();
        }
        catch
        {
            return false;
        }
    }

    public static bool IsPhoneValid(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = new string(value.Where(char.IsDigit).ToArray());
        return normalized.Length is >= 10 and <= 15;
    }
}
