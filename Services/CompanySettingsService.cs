
using Microsoft.Data.Sqlite;
using SmartErpDesktop.Models;
using System.IO;

namespace SmartErpDesktop.Services;

public sealed class CompanySettingsService
{
    private readonly string connectionString;

    public CompanySettingsService()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "SmartErpDesktop");

        Directory.CreateDirectory(folder);

        string databasePath = Path.Combine(folder, "smarterp.db");

        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = """
            CREATE TABLE IF NOT EXISTS CompanySettings
            (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                StoreName TEXT NOT NULL DEFAULT '',
                Phone TEXT NOT NULL DEFAULT '',
                Address TEXT NOT NULL DEFAULT '',
                TaxNumber TEXT NOT NULL DEFAULT '',
                PrinterName TEXT NOT NULL DEFAULT '',
                PaperSize TEXT NOT NULL DEFAULT 'حراري 80 مم',
                ShowStoreInfo INTEGER NOT NULL DEFAULT 1,
                ShowPrintPreview INTEGER NOT NULL DEFAULT 1,
                ShowRepresentativeOnCustomerInvoice INTEGER NOT NULL DEFAULT 1,
                ShowRepresentativeOnInternalInvoice INTEGER NOT NULL DEFAULT 0,
                Theme TEXT NOT NULL DEFAULT 'فاتح'
            );

            INSERT OR IGNORE INTO CompanySettings (Id)
            VALUES (1);
            """;

        command.ExecuteNonQuery();

        using var columns = connection.CreateCommand();
        columns.CommandText = "PRAGMA table_info(CompanySettings);";
        using var reader = columns.ExecuteReader();
        var hasCustomerRepresentativeSetting = false;
        var hasInternalRepresentativeSetting = false;
        while (reader.Read())
        {
            if (reader.GetString(1) == "ShowRepresentativeOnCustomerInvoice") hasCustomerRepresentativeSetting = true;
            if (reader.GetString(1) == "ShowRepresentativeOnInternalInvoice") hasInternalRepresentativeSetting = true;
        }
        reader.Close();
        if (!hasCustomerRepresentativeSetting)
        {
            using var alter = connection.CreateCommand();
            alter.CommandText = "ALTER TABLE CompanySettings ADD COLUMN ShowRepresentativeOnCustomerInvoice INTEGER NOT NULL DEFAULT 1;";
            alter.ExecuteNonQuery();
        }
        if (!hasInternalRepresentativeSetting)
        {
            using var alter = connection.CreateCommand();
            alter.CommandText = "ALTER TABLE CompanySettings ADD COLUMN ShowRepresentativeOnInternalInvoice INTEGER NOT NULL DEFAULT 0;";
            alter.ExecuteNonQuery();
        }
    }

    public CompanySettings GetSettings()
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT StoreName, Phone, Address, TaxNumber,
                   PrinterName, PaperSize, ShowStoreInfo,
                   ShowPrintPreview, Theme,
                   ShowRepresentativeOnCustomerInvoice, ShowRepresentativeOnInternalInvoice
            FROM CompanySettings
            WHERE Id = 1;
            """;

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return new CompanySettings();

        return new CompanySettings
        {
            Id = 1,
            StoreName = reader.GetString(0),
            Phone = reader.GetString(1),
            Address = reader.GetString(2),
            TaxNumber = reader.GetString(3),
            PrinterName = reader.GetString(4),
            PaperSize = reader.GetString(5),
            ShowStoreInfo = reader.GetInt32(6) == 1,
            ShowPrintPreview = reader.GetInt32(7) == 1,
            Theme = reader.GetString(8),
            ShowRepresentativeOnCustomerInvoice = reader.GetInt32(9) == 1,
            ShowRepresentativeOnInternalInvoice = reader.GetInt32(10) == 1
        };
    }

    public void SaveSettings(CompanySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = """
            INSERT INTO CompanySettings
            (
                Id, StoreName, Phone, Address, TaxNumber,
                PrinterName, PaperSize, ShowStoreInfo,
                ShowPrintPreview, Theme, ShowRepresentativeOnCustomerInvoice,
                ShowRepresentativeOnInternalInvoice
            )
            VALUES
            (
                1, $name, $phone, $address, $tax,
                $printer, $paper, $showInfo, $preview, $theme, $showRepCustomer, $showRepInternal
            )
            ON CONFLICT(Id) DO UPDATE SET
                StoreName = excluded.StoreName,
                Phone = excluded.Phone,
                Address = excluded.Address,
                TaxNumber = excluded.TaxNumber,
                PrinterName = excluded.PrinterName,
                PaperSize = excluded.PaperSize,
                ShowStoreInfo = excluded.ShowStoreInfo,
                ShowPrintPreview = excluded.ShowPrintPreview,
                ShowRepresentativeOnCustomerInvoice = excluded.ShowRepresentativeOnCustomerInvoice,
                ShowRepresentativeOnInternalInvoice = excluded.ShowRepresentativeOnInternalInvoice,
                Theme = excluded.Theme;
            """;

        command.Parameters.AddWithValue("$name", settings.StoreName.Trim());
        command.Parameters.AddWithValue("$phone", settings.Phone.Trim());
        command.Parameters.AddWithValue("$address", settings.Address.Trim());
        command.Parameters.AddWithValue("$tax", settings.TaxNumber.Trim());
        command.Parameters.AddWithValue("$printer", settings.PrinterName.Trim());
        command.Parameters.AddWithValue("$paper", settings.PaperSize);
        command.Parameters.AddWithValue("$showInfo", settings.ShowStoreInfo ? 1 : 0);
        command.Parameters.AddWithValue("$preview", settings.ShowPrintPreview ? 1 : 0);
        command.Parameters.AddWithValue("$showRepCustomer", settings.ShowRepresentativeOnCustomerInvoice ? 1 : 0);
        command.Parameters.AddWithValue("$showRepInternal", settings.ShowRepresentativeOnInternalInvoice ? 1 : 0);
        command.Parameters.AddWithValue("$theme", settings.Theme);

        command.ExecuteNonQuery();
    }
}
