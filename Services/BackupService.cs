using Microsoft.Data.Sqlite;
using System.IO;

namespace SmartErpDesktop.Services;

/// <summary>Creates and restores consistent SQLite backups of the complete ERP database.</summary>
public sealed class BackupService
{
    public string DatabasePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SmartErpDesktop", "smarterp.db");

    public void CreateBackup(string destinationPath)
    {
        if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("حدد مكان حفظ النسخة الاحتياطية.");
        if (!File.Exists(DatabasePath)) throw new FileNotFoundException("قاعدة بيانات البرنامج غير موجودة.", DatabasePath);
        var sourceBuilder = new SqliteConnectionStringBuilder { DataSource = DatabasePath, Mode = SqliteOpenMode.ReadOnly };
        var destinationBuilder = new SqliteConnectionStringBuilder { DataSource = destinationPath, Mode = SqliteOpenMode.ReadWriteCreate };
        using var source = new SqliteConnection(sourceBuilder.ToString());
        using var destination = new SqliteConnection(destinationBuilder.ToString());
        source.Open(); destination.Open();
        source.BackupDatabase(destination);
    }

    public void RestoreBackup(string backupPath)
    {
        if (string.IsNullOrWhiteSpace(backupPath) || !File.Exists(backupPath))
            throw new FileNotFoundException("ملف النسخة الاحتياطية غير موجود.", backupPath);
        if (string.Equals(Path.GetFullPath(backupPath), Path.GetFullPath(DatabasePath), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("اختار ملف نسخة احتياطية مختلف عن قاعدة البيانات الحالية.");
        var sourceBuilder = new SqliteConnectionStringBuilder { DataSource = backupPath, Mode = SqliteOpenMode.ReadOnly };
        var destinationBuilder = new SqliteConnectionStringBuilder { DataSource = DatabasePath, Mode = SqliteOpenMode.ReadWriteCreate };
        using var source = new SqliteConnection(sourceBuilder.ToString());
        using var destination = new SqliteConnection(destinationBuilder.ToString());
        source.Open(); destination.Open();
        using (var check = source.CreateCommand())
        {
            check.CommandText = "PRAGMA quick_check;";
            var result = check.ExecuteScalar()?.ToString();
            if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("ملف النسخة الاحتياطية غير سليم: " + result);
        }
        source.BackupDatabase(destination);
    }
}
