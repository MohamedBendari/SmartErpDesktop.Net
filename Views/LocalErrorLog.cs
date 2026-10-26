using System.Text;
using System.IO;

namespace SmartErpDesktop.Views;

/// <summary>Stores structural diagnostics locally without recording customer, invoice, or financial values.</summary>
internal static class LocalErrorLog
{
    internal static void Write(string operation, Exception exception)
    {
        try
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SmartErpDesktop", "logs");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, "runtime-errors.log");
            var entry = new StringBuilder()
                .AppendLine($"[{DateTimeOffset.UtcNow:O}] Operation: {operation}");

            for (Exception? current = exception; current is not null; current = current.InnerException)
            {
                entry.AppendLine($"ExceptionType: {current.GetType().FullName}");
                entry.AppendLine($"HResult: 0x{current.HResult:X8}");
                entry.AppendLine("StackTrace:");
                entry.AppendLine(current.StackTrace ?? "<not available>");
                if (current.InnerException is not null) entry.AppendLine("InnerException:");
            }

            entry.AppendLine();
            File.AppendAllText(path, entry.ToString(), Encoding.UTF8);
        }
        catch
        {
            // Logging must never prevent the user from returning to the application.
        }
    }
}
