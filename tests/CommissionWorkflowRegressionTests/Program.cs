using System.Collections;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using SmartErpDesktop.Models;
using SmartErpDesktop.Services;
using SmartErpDesktop.Views;

internal static class Program
{
    private const string RepresentativeCode = "REGRESSION-REP";

    [STAThread]
    private static int Main()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), "SmartErpCommissionWorkflow-" + Guid.NewGuid().ToString("N"), "test.db");
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        try
        {
            var service = new MockErpService(databasePath);
            var representative = new Representative { Code = RepresentativeCode, Name = "Regression, \"Rep\"\nمندوب", IsActive = true };
            var customer = new Customer { Code = "REGRESSION-CUSTOMER", Name = "Regression Customer", Type = "فرد" };
            service.Representatives.Add(representative);
            service.Customers.Add(customer);
            service.SaveCommissionRule(null, RepresentativeCode, "Regression default rule", "", "", 2.5m, 100, true);

            var product = service.Products.Single(x => x.Code == "P-1001");
            var invoice = service.SaveSale(
                new List<InvoiceLine> { new() { ProductCode = product.Code, ProductName = product.Name, Quantity = 1, UnitPrice = 1000m } },
                "كاش", customer, 200m, 0m, representativeCode: RepresentativeCode, initialPaymentMethod: "نقدي");
            service.Collect(customer, 200m, "نقدي", "second commission accrual");

            var accruals = service.CommissionAccruals.OrderBy(x => x.CollectedAt).ToList();
            Check(accruals.Count == 2 && accruals.All(x => x.CommissionAmount == 5m), "two earned accruals are available for the representative");
            Check(service.GetAccruedCommission(RepresentativeCode) == 10m && service.GetApprovedCommission(RepresentativeCode) == 0m, "earned and approved totals start separately");
            var initialSummary = service.GetCommissionSummary(RepresentativeCode);
            Check(initialSummary.Earned == 10m && initialSummary.Approved == 0m && initialSummary.Unapproved == 10m && initialSummary.Paid == 0m && initialSummary.Outstanding == 0m &&
                  initialSummary.AccrualCount == 2 && initialSummary.ApprovalCount == 0 && initialSummary.PayoutReceiptCount == 0,
                "shared summary reconciles earned, unapproved, and record counts before approval");

            var view = new RepresentativeDetailsView(service, representative, () => { });
            var tabs = Descendants<TabControl>(view).Single();
            var accountTab = (TabItem)tabs.Items[6];
            var reportView = (CommissionReportView)((TabItem)tabs.Items[4]).Content;
            var reportGrid = Descendants<DataGrid>(reportView).Single();
            var reportInvoice = GetField<TextBox>(reportView, "invoice");
            var reportSource = GetField<TextBox>(reportView, "source");
            var reportFrom = GetField<DatePicker>(reportView, "from");
            var reportTo = GetField<DatePicker>(reportView, "to");
            var reportStatus = GetField<ComboBox>(reportView, "status");
            var reportRepresentative = GetField<ComboBox>(reportView, "rep");
            var reportPayout = GetField<TextBox>(reportView, "payout");
            Check(ReportRowCount(reportGrid) == 2, "commission report initially includes both saved accrual rows");
            reportInvoice.Text = "no-such-invoice";
            Check(ReportRowCount(reportGrid) == 0 && GetField<TextBlock>(reportView, "totals").Text.Contains("Accruals 0"), "invoice filter and totals handle empty results");
            var emptyCsv = (string)(typeof(CommissionReportView).GetMethod("BuildCsv", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(reportView, null)
                ?? throw new InvalidOperationException("Commission CSV builder was not found."));
            Check(ParseCsv(emptyCsv)[^1][19] == "0", "empty filtered CSV emits zero totals and counts");
            reportInvoice.Clear();
            reportSource.Text = accruals[0].PaymentReceiptNumber;
            Check(ReportRowCount(reportGrid) == 1, "collection receipt filter follows the saved accrual source reference");
            reportSource.Clear();
            reportFrom.SelectedDate = accruals.Min(x => x.CollectedAt).Date;
            reportTo.SelectedDate = accruals.Max(x => x.CollectedAt).Date;
            Check(ReportRowCount(reportGrid) == 2, "inclusive date range retains records on both selected boundaries");
            reportFrom.SelectedDate = null; reportTo.SelectedDate = null;
            reportRepresentative.SelectedIndex = 1;
            Check(ReportRowCount(reportGrid) == 2, "representative filter returns that representative's persisted accruals");
            reportRepresentative.SelectedIndex = 0;
            reportStatus.SelectedItem = "Earned";
            Check(ReportRowCount(reportGrid) == 2, "earned status filter selects unapproved accruals");
            reportStatus.SelectedIndex = 0;
            var csv = (string)(typeof(CommissionReportView).GetMethod("BuildCsv", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(reportView, null)
                ?? throw new InvalidOperationException("Commission CSV builder was not found."));
            var csvRows = ParseCsv(csv);
            var csvTotals = csvRows[^1];
            Check(csvRows[1][1] == representative.Name, "CSV correctly escapes quotes, commas, embedded newlines, and Arabic text");
            Check(csvTotals[9] == "400" && csvTotals[10] == "10" && csvTotals[11] == "0" && csvTotals[12] == "10" &&
                  csvTotals[16] == "0" && csvTotals[18] == "0" && csvTotals[19] == "2" && csvTotals[20] == "0" && csvTotals[21] == "0",
                "CSV totals match the filtered screen summary and record counts");

            service.ApproveCommissionAccruals(new[] { accruals[0].Id }, "regression approver");
            Check(service.CommissionApprovals.Count == 1 && service.GetApprovedCommission(RepresentativeCode) == 5m, "approving one accrual records exactly one approval");
            var duplicateRejected = false;
            try { service.ApproveCommissionAccruals(new[] { accruals[0].Id }, "duplicate approver"); }
            catch (InvalidOperationException) { duplicateRejected = true; }
            Check(duplicateRejected && service.CommissionApprovals.Count == 1, "duplicate approval is rejected without adding another event");

            var excessPayoutRejected = false;
            try { service.RecordRepresentativePayout(RepresentativeCode, 5.01m, "نقدي", "over outstanding"); }
            catch (InvalidOperationException) { excessPayoutRejected = true; }
            Check(excessPayoutRejected && service.RepresentativePayouts.Count == 0, "payout above approved outstanding is rejected");

            var firstPayout = service.RecordRepresentativePayout(RepresentativeCode, 3m, "نقدي", "partial payout");
            reportPayout.Text = firstPayout.ReceiptNumber;
            Check(ReportRowCount(reportGrid) == 1, "payout receipt filter follows saved payout allocations");
            reportPayout.Clear();
            Check(firstPayout.Allocations.Count == 1 && firstPayout.Allocations[0].AccrualId == accruals[0].Id && firstPayout.Allocations[0].Amount == 3m,
                "partial payout allocates only against the approved accrual");
            Check(service.GetPaidCommission(RepresentativeCode) == 3m && service.GetOutstandingCommission(RepresentativeCode) == 2m,
                "paid and approved outstanding totals agree after partial payout");
            var partialSummary = service.GetCommissionSummary(RepresentativeCode);
            Check(partialSummary.Earned == 10m && partialSummary.Approved == 5m && partialSummary.Unapproved == 5m && partialSummary.Paid == 3m &&
                  partialSummary.Outstanding == 2m && partialSummary.AccrualCount == 2 && partialSummary.ApprovalCount == 1 && partialSummary.PayoutReceiptCount == 1,
                "shared summary reconciles partial payout allocations and receipt counts");
            Check(service.GetCommissionAccrualStatus(accruals[0]) == CommissionAccrualStatus.PartiallyPaid && service.GetCommissionAccrualStatus(accruals[1]) == CommissionAccrualStatus.Earned,
                "partial payout and unapproved accrual statuses remain distinct");

            tabs.SelectedIndex = 6;
            tabs.SelectedItem = accountTab;
            var account = ReadAccountRows(accountTab);
            Check(account.AccrualRows.Count == 2 && account.AccrualRows.Count(x => x.Status == "مدفوعة جزئياً") == 1 && account.AccrualRows.Count(x => x.Status == "مستحقة") == 1,
                "account statement refresh shows current approval and partial-payment statuses");
            Check(account.AccrualRows.Sum(x => x.Earned) == 10m && account.PayoutRows.Sum() == 3m,
                "account statement earned and payout rows match persisted totals");
            Check(account.Summary.Contains("10.00") && account.Summary.Contains("5.00") && account.Summary.Contains("3.00") && account.Summary.Contains("2.00"),
                "account statement summary matches accrued, approved, paid, and outstanding totals");

            service.ApproveCommissionAccruals(new[] { accruals[1].Id }, "regression approver");
            var secondPayout = service.RecordRepresentativePayout(RepresentativeCode, 5m, "نقدي", "partial across accruals");
            Check(secondPayout.Allocations.Count == 2 && secondPayout.Allocations[0].AccrualId == accruals[0].Id && secondPayout.Allocations[0].Amount == 2m &&
                  secondPayout.Allocations[1].AccrualId == accruals[1].Id && secondPayout.Allocations[1].Amount == 3m,
                "later payout uses remaining approved balances in accrual order");
            Check(service.GetPaidCommission(RepresentativeCode) == 8m && service.GetOutstandingCommission(RepresentativeCode) == 2m,
                "partial payouts leave the correct approved outstanding balance");
            var finalSummary = service.GetCommissionSummary(RepresentativeCode);
            Check(finalSummary.Approved == 10m && finalSummary.Unapproved == 0m && finalSummary.Paid == 8m && finalSummary.Outstanding == 2m &&
                  finalSummary.ApprovalCount == 2 && finalSummary.PayoutReceiptCount == 2,
                "shared summary reconciles multiple payout receipts across multiple accruals");

            tabs.SelectedIndex = 6;
            tabs.SelectedItem = accountTab;
            account = ReadAccountRows(accountTab);
            Check(account.AccrualRows.Count(x => x.Status == "مدفوعة") == 1 && account.AccrualRows.Count(x => x.Status == "مدفوعة جزئياً") == 1,
                "account statement refresh reflects paid and partially paid accruals");
            Check(account.AccrualRows.Sum(x => x.Earned) == 10m && account.PayoutRows.Sum() == 8m &&
                  service.GetApprovedCommission(RepresentativeCode) - account.PayoutRows.Sum() == service.GetOutstandingCommission(RepresentativeCode),
                "final account statement totals reconcile to approved less paid outstanding");

            service.SaveChanges();
            var reloaded = new MockErpService(databasePath);
            Check(reloaded.CommissionApprovals.Count == 2 && reloaded.RepresentativePayouts.Count == 2 && reloaded.RepresentativePayouts.Sum(x => x.Allocations.Sum(a => a.Amount)) == 8m,
                "approval and payout allocation records persist and reload without duplication");
            Check(reloaded.Invoices.Single(x => x.Number == invoice.Number).Paid == 400m && reloaded.Payments.Sum(x => x.Amount) == 400m,
                "commission workflow leaves customer invoice and payment totals unchanged");

            var installmentCustomer = new Customer { Code = "REGRESSION-INSTALLMENT-CUSTOMER", Name = "Installment Regression Customer", Type = customer.Type, Balance = 1000m };
            service.Customers.Add(installmentCustomer);
            var installmentInvoice = service.SaveSale(
                new List<InvoiceLine> { new() { ProductCode = product.Code, ProductName = product.Name, Quantity = 1, UnitPrice = 1000m } },
                service.CommissionSaleTypes[1], installmentCustomer, 200m, 0m, installmentMonths: 1,
                schedule: new[] { new Installment { DueDate = DateTime.Today.AddDays(30), Amount = 800m } }, representativeCode: RepresentativeCode);
            var installmentAccruals = service.CommissionAccruals.Where(x => x.InvoiceNumber == installmentInvoice.Number).ToList();
            Check(installmentAccruals.Count == 1 && installmentAccruals[0].CommissionAmount == 5m && installmentAccruals[0].RatePercent == 2.5m,
                "installment invoice initial payment accrues from its saved rule snapshot");
            var installment = service.Installments.Single(x => x.InvoiceNumber == installmentInvoice.Number);
            service.CollectForInstallment(installment, 100m, "cash", "installment commission regression");
            installmentAccruals = service.CommissionAccruals.Where(x => x.InvoiceNumber == installmentInvoice.Number).OrderBy(x => x.CollectedAt).ToList();
            Check(installmentAccruals.Count == 2 && installmentAccruals[1].CommissionAmount == 2.5m &&
                  installmentAccruals.Select(x => x.SourceAllocationId).Distinct().Count() == 2,
                "installment collection creates one additional accrual without duplicating its source");
            service.SaveChanges();
            var installmentReloaded = new MockErpService(databasePath);
            var persistedInstallmentAccruals = installmentReloaded.CommissionAccruals.Where(x => x.InvoiceNumber == installmentInvoice.Number).ToList();
            Check(persistedInstallmentAccruals.Count == 2 && persistedInstallmentAccruals.All(x => x.RatePercent == 2.5m && x.RuleDescription == "Regression default rule"),
                "initial and installment collection accrual snapshots persist and reload unchanged");

            Console.WriteLine("All commission approval and payout regression checks passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static (List<(string Status, decimal Earned)> AccrualRows, List<decimal> PayoutRows, string Summary) ReadAccountRows(TabItem accountTab)
    {
        var grids = Descendants<DataGrid>(accountTab).ToList();
        var grid = grids.Single();
        var rows = ((IEnumerable)grid.ItemsSource!).Cast<object>().ToList();
        var accrualRows = rows.Where(x => (string)GetProperty(x, "Type")! == "استحقاق عمولة")
            .Select(x => ((string)GetProperty(x, "Status")!, (decimal)GetProperty(x, "Earned")!)).ToList();
        var payoutRows = rows.Where(x => (string)GetProperty(x, "Type")! == "صرف عمولة")
            .Select(x => (decimal)GetProperty(x, "Paid")!).ToList();
        var summary = Descendants<TextBlock>(accountTab).Select(x => x.Text).First(x => x.Contains("العمولات المستحقة"));
        return (accrualRows, payoutRows, summary);
    }

    private static object? GetProperty(object instance, string name) =>
        instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(instance);

    private static T GetField<T>(object instance, string name) where T : class =>
        (T)(instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance)
            ?? throw new InvalidOperationException("Missing test field: " + name));

    private static int ReportRowCount(DataGrid grid) => ((IEnumerable)grid.ItemsSource!).Cast<object>().Count();

    private static List<List<string>> ParseCsv(string csv)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new System.Text.StringBuilder();
        var quoted = false;
        for (var i = 0; i < csv.Length; i++)
        {
            var c = csv[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < csv.Length && csv[i + 1] == '"') { field.Append('"'); i++; }
                else if (c == '"') quoted = false;
                else field.Append(c);
            }
            else if (c == '"') quoted = true;
            else if (c == ',') { row.Add(field.ToString()); field.Clear(); }
            else if (c == '\n') { row.Add(field.ToString().TrimEnd('\r')); field.Clear(); rows.Add(row); row = new List<string>(); }
            else field.Append(c);
        }
        if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); rows.Add(row); }
        return rows;
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match) yield return match;
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
    }

    private static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + description);
        Console.WriteLine("PASS: " + description);
    }
}
