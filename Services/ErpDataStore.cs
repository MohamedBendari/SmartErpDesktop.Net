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
    private static readonly string DefaultDatabasePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SmartErpDesktop", "smarterp.db");

    private static string BuildConnectionString(string? databasePath) => new SqliteConnectionStringBuilder
    {
        DataSource = string.IsNullOrWhiteSpace(databasePath) ? DefaultDatabasePath : Path.GetFullPath(databasePath),
        Mode = SqliteOpenMode.ReadWriteCreate,
        ForeignKeys = true
    }.ToString();

    private static string ResolveDatabasePath(string? databasePath) =>
        string.IsNullOrWhiteSpace(databasePath) ? DefaultDatabasePath : Path.GetFullPath(databasePath);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal sealed class State
    {
        public List<Product> Products { get; set; } = new();
        public List<Customer> Customers { get; set; } = new();
        public List<Representative> Representatives { get; set; } = new();
        public List<CommissionRule> CommissionRules { get; set; } = new();
        public List<CommissionAccrual> CommissionAccruals { get; set; } = new();
        public List<CommissionApproval> CommissionApprovals { get; set; } = new();
        public List<RepresentativePayout> RepresentativePayouts { get; set; } = new();
        public List<Invoice> Invoices { get; set; } = new();
        public List<Payment> Payments { get; set; } = new();
        public List<Installment> Installments { get; set; } = new();
        public List<Supplier> Suppliers { get; set; } = new();
        public List<SupplierPayment> SupplierPayments { get; set; } = new();
        public List<SupplierInstallment> SupplierInstallments { get; set; } = new();
        public List<Purchase> Purchases { get; set; } = new();
        public List<StockMovement> Movements { get; set; } = new();
        public List<Expense> Expenses { get; set; } = new();
        public PartnerData Partners { get; set; } = new();
    }

    internal sealed class PartnerData
    {
        public List<Partner> Partners { get; set; } = new();
        public List<PartnerTransaction> Transactions { get; set; } = new();
        public List<PartnerProfitAllocation> Allocations { get; set; } = new();
        public List<PartnerExitSettlement> Settlements { get; set; } = new();
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
                SupplierPaymentsJson TEXT NOT NULL DEFAULT '[]',
                SupplierInstallmentsJson TEXT NOT NULL DEFAULT '[]',
                PartnersJson TEXT NOT NULL DEFAULT '{}',
                RepresentativesJson TEXT NOT NULL DEFAULT '[]',
                CommissionRulesJson TEXT NOT NULL DEFAULT '[]',
                CommissionAccrualsJson TEXT NOT NULL DEFAULT '[]',
                CommissionApprovalsJson TEXT NOT NULL DEFAULT '[]',
                RepresentativePayoutsJson TEXT NOT NULL DEFAULT '[]',
                UpdatedAt TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
        using var columns = connection.CreateCommand();
        columns.CommandText = "PRAGMA table_info(ErpOperationalData);";
        using var reader = columns.ExecuteReader();
        var hasSupplierPayments = false;
        var hasSupplierInstallments = false;
        var hasPartners = false;
        var hasRepresentatives = false;
        var hasCommissionRules = false;
        var hasCommissionAccruals = false;
        var hasCommissionApprovals = false;
        var hasRepresentativePayouts = false;
        while (reader.Read()) { if (reader.GetString(1) == "SupplierPaymentsJson") hasSupplierPayments = true; if (reader.GetString(1) == "SupplierInstallmentsJson") hasSupplierInstallments = true; if (reader.GetString(1) == "PartnersJson") hasPartners = true; if (reader.GetString(1) == "RepresentativesJson") hasRepresentatives = true; if (reader.GetString(1) == "CommissionRulesJson") hasCommissionRules = true; if (reader.GetString(1) == "CommissionAccrualsJson") hasCommissionAccruals = true; if (reader.GetString(1) == "CommissionApprovalsJson") hasCommissionApprovals = true; if (reader.GetString(1) == "RepresentativePayoutsJson") hasRepresentativePayouts = true; }
        reader.Close();
        if (!hasSupplierPayments)
        {
            using var alter = connection.CreateCommand();
            alter.CommandText = "ALTER TABLE ErpOperationalData ADD COLUMN SupplierPaymentsJson TEXT NOT NULL DEFAULT '[]';";
            alter.ExecuteNonQuery();
        }
        if (!hasSupplierInstallments) { using var alter = connection.CreateCommand(); alter.CommandText = "ALTER TABLE ErpOperationalData ADD COLUMN SupplierInstallmentsJson TEXT NOT NULL DEFAULT '[]';"; alter.ExecuteNonQuery(); }
        if (!hasPartners) { using var alter = connection.CreateCommand(); alter.CommandText = "ALTER TABLE ErpOperationalData ADD COLUMN PartnersJson TEXT NOT NULL DEFAULT '{}';"; alter.ExecuteNonQuery(); }
        if (!hasRepresentatives) { using var alter = connection.CreateCommand(); alter.CommandText = "ALTER TABLE ErpOperationalData ADD COLUMN RepresentativesJson TEXT NOT NULL DEFAULT '[]';"; alter.ExecuteNonQuery(); }
        if (!hasCommissionRules) { using var alter = connection.CreateCommand(); alter.CommandText = "ALTER TABLE ErpOperationalData ADD COLUMN CommissionRulesJson TEXT NOT NULL DEFAULT '[]';"; alter.ExecuteNonQuery(); }
        if (!hasCommissionAccruals) { using var alter = connection.CreateCommand(); alter.CommandText = "ALTER TABLE ErpOperationalData ADD COLUMN CommissionAccrualsJson TEXT NOT NULL DEFAULT '[]';"; alter.ExecuteNonQuery(); }
        if (!hasCommissionApprovals) { using var alter = connection.CreateCommand(); alter.CommandText = "ALTER TABLE ErpOperationalData ADD COLUMN CommissionApprovalsJson TEXT NOT NULL DEFAULT '[]';"; alter.ExecuteNonQuery(); }
        if (!hasRepresentativePayouts) { using var alter = connection.CreateCommand(); alter.CommandText = "ALTER TABLE ErpOperationalData ADD COLUMN RepresentativePayoutsJson TEXT NOT NULL DEFAULT '[]';"; alter.ExecuteNonQuery(); }
    }

    internal static bool TryLoad(MockErpService service, out State state, string? databasePath = null)
    {
        var resolvedPath = ResolveDatabasePath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(resolvedPath)!);
        using var connection = new SqliteConnection(BuildConnectionString(resolvedPath));
        connection.Open();
        EnsureSchema(connection);

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ProductsJson, CustomersJson, InvoicesJson, PaymentsJson,
                   InstallmentsJson, SuppliersJson, PurchasesJson, MovementsJson, ExpensesJson, SupplierPaymentsJson, SupplierInstallmentsJson, PartnersJson, RepresentativesJson, CommissionRulesJson, CommissionAccrualsJson, CommissionApprovalsJson, RepresentativePayoutsJson
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
            Expenses = Deserialize<List<Expense>>(reader.GetString(8)),
            SupplierPayments = Deserialize<List<SupplierPayment>>(reader.GetString(9)),
            SupplierInstallments = Deserialize<List<SupplierInstallment>>(reader.GetString(10)),
            Partners = Deserialize<PartnerData>(reader.GetString(11)),
            Representatives = Deserialize<List<Representative>>(reader.GetString(12)),
            CommissionRules = Deserialize<List<CommissionRule>>(reader.GetString(13)),
            CommissionAccruals = Deserialize<List<CommissionAccrual>>(reader.GetString(14)),
            CommissionApprovals = Deserialize<List<CommissionApproval>>(reader.GetString(15)),
            RepresentativePayouts = Deserialize<List<RepresentativePayout>>(reader.GetString(16))
        };
        return true;
    }

    internal static void Save(MockErpService service, string? databasePath = null)
    {
        var resolvedPath = ResolveDatabasePath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(resolvedPath)!);
        using var connection = new SqliteConnection(BuildConnectionString(resolvedPath));
        connection.Open();
        EnsureSchema(connection);
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO ErpOperationalData
            (Id, SchemaVersion, ProductsJson, CustomersJson, InvoicesJson, PaymentsJson,
             InstallmentsJson, SuppliersJson, PurchasesJson, MovementsJson, ExpensesJson, SupplierPaymentsJson, SupplierInstallmentsJson, PartnersJson, RepresentativesJson, CommissionRulesJson, CommissionAccrualsJson, CommissionApprovalsJson, RepresentativePayoutsJson, UpdatedAt)
            VALUES (1, 4, $products, $customers, $invoices, $payments, $installments,
                    $suppliers, $purchases, $movements, $expenses, $supplierPayments, $supplierInstallments, $partners, $representatives, $commissionRules, $commissionAccruals, $commissionApprovals, $representativePayouts, $updatedAt)
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
                SupplierPaymentsJson = excluded.SupplierPaymentsJson,
                SupplierInstallmentsJson = excluded.SupplierInstallmentsJson,
                PartnersJson = excluded.PartnersJson,
                RepresentativesJson = excluded.RepresentativesJson,
                CommissionRulesJson = excluded.CommissionRulesJson,
                CommissionAccrualsJson = excluded.CommissionAccrualsJson,
                CommissionApprovalsJson = excluded.CommissionApprovalsJson,
                RepresentativePayoutsJson = excluded.RepresentativePayoutsJson,
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
        command.Parameters.AddWithValue("$supplierPayments", Serialize(service.SupplierPayments));
        command.Parameters.AddWithValue("$supplierInstallments", Serialize(service.SupplierInstallments));
        command.Parameters.AddWithValue("$partners", Serialize(new PartnerData { Partners = service.Partners, Transactions = service.PartnerTransactions, Allocations = service.PartnerAllocations, Settlements = service.PartnerSettlements }));
        command.Parameters.AddWithValue("$representatives", Serialize(service.Representatives));
        command.Parameters.AddWithValue("$commissionRules", Serialize(service.CommissionRules));
        command.Parameters.AddWithValue("$commissionAccruals", Serialize(service.CommissionAccruals));
        command.Parameters.AddWithValue("$commissionApprovals", Serialize(service.CommissionApprovals));
        command.Parameters.AddWithValue("$representativePayouts", Serialize(service.RepresentativePayouts));
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
