namespace SmartErpDesktop.Views;

internal static class ArabicUi
{
    public static string ErrorMessage(Exception exception, string fallback)
    {
        var message = exception.Message;
        var hasArabic = message.Any(character => character is >= '\u0600' and <= '\u06FF');
        if (hasArabic && exception is InvalidOperationException or ArgumentException)
            return message;
        return fallback;
    }
}
