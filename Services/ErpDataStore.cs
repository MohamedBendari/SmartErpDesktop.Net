using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using SmartErpDesktop.Models;

namespace SmartErpDesktop.Services;

/// <summary>
/// SQLite persistence for ERP operational data. Uses the existing smarterp.db file
/// without changing or deleting CompanySettings. Each save replaces the operational
/// snapshot inside one SQLite transaction.
/// </summary>
internal static class ErpDataStore
{
    private static readonly string DatabasePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SmartErpDesktop", "smarterp.db");

    private static readonly string ConnectionString = new SqliteConnectionStringBuilder
    {
        DataSource = DatabasePath,
        Mode = SqliteOpenMode.ReadWriteCreate,
        ForeignKeys = true
    }.ToString();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal sealed class State
    {
        public List<Product> Products { get; set; } = new();
        public List<Customer> Customers { get; set; } = new();
        public List<Invoice> Invoices { get; set; } = new();
        public List<Payment> Payments { get; set; } = new();
        public List<Installment> Installments { get; set; } = new();
        public List<Supplier> Suppliers { get; set; } = new();
        public List<Purchase> Purchases { get; set; } = new();
        public List<StockMovement> Movements { get; set; } = new();
        public List<Expense> Expenses { get; set; } = new();
    }

    private static void EnsureSchema(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS ErpOperationalData
            (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                SchemaVersion INTEGER NOT NULL DEFAULT 1,
                ProductsJson TEXT NOT NULL,
                CustomersJson TEXT NOT NULL,
                InvoicesJson TEXT NOT NULL,
                PaymentsJson TEXT NOT NULL,
                InstallmentsJson TEXT NOT NULL,
                SuppliersJson TEXT NOT NULL,
                PurchasesJson TEXT NOT NULL,
                MovementsJson TEXT NOT NULL,
                ExpensesJson TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    internal static bool TryLoad(MockErpService service, out State state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        EnsureSchema(connection);

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ProductsJson, CustomersJson, InvoicesJson, PaymentsJson,
                   InstallmentsJson, SuppliersJson, PurchasesJson, MovementsJson, ExpensesJson
            FROM ErpOperationalData WHERE Id = 1;
            """;
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            state = new State();
            return false;
        }

        state = new State
        {
            Products = Deserialize<List<Product>>(reader.GetString(0)),
            Customers = Deserialize<List<Customer>>(reader.GetString(1)),
            Invoices = Deserialize<List<Invoice>>(reader.GetString(2)),
            Payments = Deserialize<List<Payment>>(reader.GetString(3)),
            Installments = Deserialize<List<Installment>>(reader.GetString(4)),
            Suppliers = Deserialize<List<Supplier>>(reader.GetString(5)),
            Purchases = Deserialize<List<Purchase>>(reader.GetString(6)),
            Movements = Deserialize<List<StockMovement>>(reader.GetString(7)),
            Expenses = Deserialize<List<Expense>>(reader.GetString(8))
        };
        return true;
    }

    internal static void Save(MockErpService service)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        EnsureSchema(connection);
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO ErpOperationalData
            (Id, SchemaVersion, ProductsJson, CustomersJson, InvoicesJson, PaymentsJson,
             InstallmentsJson, SuppliersJson, PurchasesJson, MovementsJson, ExpensesJson, UpdatedAt)
            VALUES (1, 1, $products, $customers, $invoices, $payments, $installments,
                    $suppliers, $purchases, $movements, $expenses, $updatedAt)
            ON CONFLICT(Id) DO UPDATE SET
                SchemaVersion = excluded.SchemaVersion,
                ProductsJson = excluded.ProductsJson,
                CustomersJson = excluded.CustomersJson,
                InvoicesJson = excluded.InvoicesJson,
                PaymentsJson = excluded.PaymentsJson,
                InstallmentsJson = excluded.InstallmentsJson,
                SuppliersJson = excluded.SuppliersJson,
                PurchasesJson = excluded.PurchasesJson,
                MovementsJson = excluded.MovementsJson,
                ExpensesJson = excluded.ExpensesJson,
                UpdatedAt = excluded.UpdatedAt;
            """;
        command.Parameters.AddWithValue("$products", Serialize(service.Products));
        command.Parameters.AddWithValue("$customers", Serialize(service.Customers));
        command.Parameters.AddWithValue("$invoices", Serialize(service.Invoices));
        command.Parameters.AddWithValue("$payments", Serialize(service.Payments));
        command.Parameters.AddWithValue("$installments", Serialize(service.Installments));
        command.Parameters.AddWithValue("$suppliers", Serialize(service.Suppliers));
        command.Parameters.AddWithValue("$purchases", Serialize(service.Purchases));
        command.Parameters.AddWithValue("$movements", Serialize(service.Movements));
        command.Parameters.AddWithValue("$expenses", Serialize(service.Expenses));
        command.Parameters.AddWithValue("$updatedAt", DateTimeOffset.Now.ToString("O"));
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    internal static int MaxSuffix(IEnumerable<string> values, string prefix) => values
        .Where(value => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        .Select(value => int.TryParse(value[prefix.Length..], out var number) ? number : 0)
        .DefaultIfEmpty(0)
        .Max();

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    private static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, JsonOptions)
        ?? throw new InvalidDataException("تعذر قراءة بيانات SMART ERP المحفوظة من قاعدة البيانات.");
}
