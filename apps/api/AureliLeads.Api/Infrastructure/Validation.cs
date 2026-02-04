using System.Net.Mail;

namespace AureliLeads.Api.Infrastructure;

public static class Validation
{
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsValidPassword(string? password, int minLength = 8)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        return password.Trim().Length >= minLength;
    }

    public static bool IsValidWebhookUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Length > 500)
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }
}
