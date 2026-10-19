namespace SmartErpDesktop.Models;

public sealed class CompanySettings
{
    public int Id { get; set; } = 1;
    public string StoreName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string TaxNumber { get; set; } = "";
    public string PrinterName { get; set; } = "";
    public string PaperSize { get; set; } = "حراري 80 مم";
    public bool ShowStoreInfo { get; set; } = true;
    public bool ShowPrintPreview { get; set; } = true;
    public string Theme { get; set; } = "فاتح";
}