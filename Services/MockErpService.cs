using SmartErpDesktop.Models;
namespace SmartErpDesktop.Services;
public sealed class MockErpService
{
    public sealed record CommissionSummary(decimal Earned, decimal Approved, decimal Unapproved, decimal Paid, decimal Outstanding, int AccrualCount, int ApprovalCount, int PayoutReceiptCount);
    public List<Product> Products { get; } = new();
    public List<Customer> Customers { get; } = new();
    public List<Representative> Representatives { get; } = new();
    public List<CommissionRule> CommissionRules { get; } = new();
    public List<CommissionAccrual> CommissionAccruals { get; } = new();
    public List<CommissionApproval> CommissionApprovals { get; } = new();
    public List<RepresentativePayout> RepresentativePayouts { get; } = new();
    public List<Invoice> Invoices { get; } = new();
    public List<Payment> Payments { get; } = new();
    public List<Installment> Installments { get; } = new();
    public List<Supplier> Suppliers { get; } = new();
    public List<SupplierPayment> SupplierPayments { get; } = new();
    public List<SupplierInstallment> SupplierInstallments { get; } = new();
    public List<Partner> Partners { get; } = new();
    public List<PartnerTransaction> PartnerTransactions { get; } = new();
    public List<PartnerProfitAllocation> PartnerAllocations { get; } = new();
    public List<PartnerExitSettlement> PartnerSettlements { get; } = new();
    public List<Purchase> Purchases { get; } = new();
    public List<StockMovement> Movements { get; } = new();
    public List<Expense> Expenses { get; } = new();
    private int expenseSequence = 0;
    private int invoiceSequence = 1004;
    private int receiptSequence = 501;
    private readonly string? dataStorePath;

    public MockErpService() : this(null)
    {
    }

    public MockErpService(string? databasePath)
    {
        dataStorePath = databasePath;
        Products.AddRange(new[]
        {
            new Product
            {
                Code = "P-1001",
                Barcode = "6221001001",
                Name = "غسالة أوتوماتيك 8 كجم",
                Category = "أجهزة منزلية",
                Price = 12500,
                Cost = 10300,
                Stock = 8,
                MinimumStock = 2
            },
            new Product
            {
                Code = "P-1002",
                Barcode = "6221001002",
                Name = "ثلاجة 14 قدم",
                Category = "أجهزة منزلية",
                Price = 18900,
                Cost = 16100,
                Stock = 5,
                MinimumStock = 2
            },
            new Product
            {
                Code = "P-1003",
                Barcode = "6221001003",
                Name = "خلاط كهربائي",
                Category = "أجهزة صغيرة",
                Price = 1450,
                Cost = 980,
                Stock = 16,
                MinimumStock = 4
            },
            new Product
            {
                Code = "P-1004",
                Barcode = "6221001004",
                Name = "مروحة ستاند",
                Category = "أجهزة صغيرة",
                Price = 2200,
                Cost = 1550,
                Stock = 3,
                MinimumStock = 4
            },
            new Product
            {
                Code = "P-1005",
                Barcode = "6221001005",
                Name = "ميكروويف 25 لتر",
                Category = "أجهزة منزلية",
                Price = 5600,
                Cost = 4700,
                Stock = 7,
                MinimumStock = 2
            }
        });
        Customers.AddRange(new[]
        {
            new Customer
            {
                Code = "C-001",
                Name = "أحمد محمد",
                Phone = "01012345678",
                Address = "كفر الشيخ",
                Balance = 7200,
                Type = "فرد"
            },
            new Customer
            {
                Code = "C-002",
                Name = "منى إبراهيم",
                Phone = "01123456789",
                Address = "دسوق",
                Balance = 0,
                Type = "فرد"
            },
            new Customer
            {
                Code = "T-001",
                Name = "مؤسسة النور التجارية",
                Phone = "01234567890",
                Address = "كفر الشيخ",
                Balance = 15400,
                Type = "تاجر"
            },
            new Customer
            {
                Code = "C-004",
                Name = "محمود السيد",
                Phone = "01555555555",
                Address = "سيدي سالم",
                Balance = 2350,
                Type = "فرد"
            }
        });
        Suppliers.AddRange(new[]
        {
            new Supplier
            {
                Code = "S-001",
                Name = "الشركة المصرية للأجهزة",
                Phone = "0401234567",
                Balance = 12000
            },
            new Supplier
            {
                Code = "S-002",
                Name = "تجارة النور",
                Phone = "01098765432",
                Balance = 0
            }
        });
        Invoices.AddRange(new[]
        {
            new Invoice
            {
                Number = "INV-1001",
                Date = DateTime.Today.AddDays(-2),
                CustomerName = "منى إبراهيم",
                Type = "كاش",
                Total = 1450,
                Paid = 1450,
                Status = "مكتملة",
                Lines = new()
                {
                    new InvoiceLine
                    {
                        ProductCode = "P-1003",
                        ProductName = "خلاط كهربائي",
                        Quantity = 1,
                        UnitPrice = 1450
                    }
                }
            },
            new Invoice
            {
                Number = "INV-1002",
                Date = DateTime.Today.AddDays(-1),
                CustomerName = "أحمد محمد",
                Type = "قسط أفراد",
                Total = 12500,
                Paid = 5300,
                Status = "قسط قائم",
                Lines = new()
                {
                    new InvoiceLine
                    {
                        ProductCode = "P-1001",
                        ProductName = "غسالة أوتوماتيك 8 كجم",
                        Quantity = 1,
                        UnitPrice = 12500
                    }
                }
            },
            new Invoice
            {
                Number = "INV-1003",
                Date = DateTime.Today,
                CustomerName = "مؤسسة النور التجارية",
                Type = "آجل تاجر",
                Total = 18900,
                Paid = 3500,
                Status = "رصيد قائم",
                Lines = new()
                {
                    new InvoiceLine
                    {
                        ProductCode = "P-1002",
                        ProductName = "ثلاجة 14 قدم",
                        Quantity = 1,
                        UnitPrice = 18900
                    }
                }
            }
        });
        Purchases.Add(new Purchase
        {
            Number = "PUR-201",
            Date = DateTime.Today.AddDays(-3),
            Supplier = "الشركة المصرية للأجهزة",
            Lines = 2,
            Total = 23500,
            Status = "مستلمة تجريبياً"
        });

        // Load operational data from the same SQLite file used by CompanySettingsService.
        // Existing settings are preserved; operational tables are separate.
        if (ErpDataStore.TryLoad(this, out var savedState, dataStorePath))
        {
            Products.Clear(); Products.AddRange(savedState.Products);
            Customers.Clear(); Customers.AddRange(savedState.Customers);
            Representatives.Clear(); Representatives.AddRange(savedState.Representatives);
            CommissionRules.Clear(); CommissionRules.AddRange(savedState.CommissionRules);
            CommissionAccruals.Clear(); CommissionAccruals.AddRange(savedState.CommissionAccruals);
            CommissionApprovals.Clear(); CommissionApprovals.AddRange(savedState.CommissionApprovals);
            RepresentativePayouts.Clear(); RepresentativePayouts.AddRange(savedState.RepresentativePayouts);
            Invoices.Clear(); Invoices.AddRange(savedState.Invoices);
            Payments.Clear(); Payments.AddRange(savedState.Payments);
            Installments.Clear(); Installments.AddRange(savedState.Installments);
            Suppliers.Clear(); Suppliers.AddRange(savedState.Suppliers);
            SupplierPayments.Clear(); SupplierPayments.AddRange(savedState.SupplierPayments);
            SupplierInstallments.Clear(); SupplierInstallments.AddRange(savedState.SupplierInstallments);
            Partners.Clear(); Partners.AddRange(savedState.Partners.Partners);
            PartnerTransactions.Clear(); PartnerTransactions.AddRange(savedState.Partners.Transactions);
            PartnerAllocations.Clear(); PartnerAllocations.AddRange(savedState.Partners.Allocations);
            PartnerSettlements.Clear(); PartnerSettlements.AddRange(savedState.Partners.Settlements);
            Purchases.Clear(); Purchases.AddRange(savedState.Purchases);
            Movements.Clear(); Movements.AddRange(savedState.Movements);
            Expenses.Clear(); Expenses.AddRange(savedState.Expenses);
            invoiceSequence = Math.Max(invoiceSequence, ErpDataStore.MaxSuffix(Invoices.Select(x => x.Number), "INV-"));
            receiptSequence = Math.Max(receiptSequence, ErpDataStore.MaxSuffix(Payments.Select(x => x.ReceiptNumber), "REC-"));
            expenseSequence = Math.Max(expenseSequence, ErpDataStore.MaxSuffix(Expenses.Select(x => x.Number), "EXP-"));
        }
        else
        {
            SaveChanges(); // First run only: persist demo seed data once.
        }
    }

    public Purchase CreatePurchase(string supplierName, IEnumerable<PurchaseLine> sourceLines, decimal paidNow, bool receiveGoods)
        => CreatePurchase(supplierName, sourceLines, paidNow, receiveGoods, 0, DateTime.Today.AddMonths(1), DateTime.Today);

    public Purchase CreatePurchase(string supplierName, IEnumerable<PurchaseLine> sourceLines, decimal paidNow, bool receiveGoods, int installmentMonths, DateTime firstDueDate)
        => CreatePurchase(supplierName, sourceLines, paidNow, receiveGoods, installmentMonths, firstDueDate, DateTime.Today);

    public Purchase CreatePurchase(string supplierName, IEnumerable<PurchaseLine> sourceLines, decimal paidNow, bool receiveGoods, int installmentMonths, DateTime firstDueDate, DateTime purchaseDate, decimal invoiceDiscount = 0)
    {
        var supplier = Suppliers.FirstOrDefault(x => x.Name == supplierName)
            ?? throw new InvalidOperationException("اختار موردًا مسجلًا أولاً.");
        var lines = sourceLines.Select(x => new PurchaseLine { ProductCode = x.ProductCode, ProductName = x.ProductName, Quantity = x.Quantity, UnitCost = x.UnitCost, Discount = x.Discount }).ToList();
        if (lines.Count == 0) throw new InvalidOperationException("أضف صنفًا واحدًا على الأقل للفاتورة.");
        foreach (var line in lines)
        {
            var product = Products.FirstOrDefault(x => x.Code == line.ProductCode) ?? throw new InvalidOperationException($"الصنف {line.ProductName} غير موجود.");
            if (line.Quantity <= 0 || line.UnitCost < 0 || line.Discount < 0 || line.Discount > line.Quantity * line.UnitCost)
                throw new InvalidOperationException($"راجع الكمية والتكلفة والخصم للصنف {product.Name}.");
        }
        var subtotal = lines.Sum(x => x.Total);
        if (invoiceDiscount < 0 || invoiceDiscount > subtotal) throw new InvalidOperationException("خصم الفاتورة يجب أن يكون بين صفر وإجمالي الأصناف.");
        var total = subtotal - invoiceDiscount;
        if (paidNow < 0 || paidNow > total) throw new InvalidOperationException("المدفوع يجب أن يكون بين صفر وإجمالي الفاتورة.");
        if (installmentMonths < 0 || installmentMonths > 120 || (installmentMonths > 0 && total - paidNow <= 0)) throw new InvalidOperationException("مدة التقسيط أو الرصيد المتبقي غير صحيح.");
        var number = "PUR-" + (ErpDataStore.MaxSuffix(Purchases.Select(x => x.Number), "PUR-") + 1).ToString("0000");
        var purchase = new Purchase { Number = number, Date = purchaseDate.Date, Supplier = supplier.Name, Lines = lines.Count, Details = lines, Total = total, Paid = paidNow, IsReceived = false, Status = "غير مستلمة" };
        purchase.InstallmentMonths = installmentMonths;
        purchase.InitialPayment = paidNow;
        purchase.FirstInstallmentDueDate = installmentMonths > 0 ? firstDueDate.Date : null;
        purchase.InvoiceDiscount = invoiceDiscount;
        Purchases.Add(purchase);
        supplier.Balance += total - paidNow;
        if (installmentMonths > 0)
        {
            var financed = total - paidNow;
            var each = decimal.Round(financed / installmentMonths, 2, MidpointRounding.AwayFromZero);
            for (var i = 0; i < installmentMonths; i++)
                SupplierInstallments.Add(new SupplierInstallment { PurchaseNumber = number, SupplierName = supplier.Name, Sequence = i + 1, DueDate = firstDueDate.Date.AddMonths(i), Amount = i == installmentMonths - 1 ? financed - each * (installmentMonths - 1) : each });
        }
        if (paidNow > 0) SupplierPayments.Add(new SupplierPayment { ReceiptNumber = "SP-" + (ErpDataStore.MaxSuffix(SupplierPayments.Select(x => x.ReceiptNumber), "SP-") + 1).ToString("0000"), Date = DateTime.Now, SupplierName = supplier.Name, PurchaseNumber = number, Amount = paidNow, BalanceBefore = supplier.Balance + paidNow - total, BalanceAfter = supplier.Balance, Method = "نقدي", Note = "دفعة عند إنشاء فاتورة الشراء" });
        if (receiveGoods) ReceivePurchase(purchase, false);
        SaveChanges();
        return purchase;
    }

    public void ReceivePurchase(Purchase purchase) => ReceivePurchase(purchase, true);
    private void ReceivePurchase(Purchase purchase, bool save)
    {
        if (purchase.IsReceived && purchase.Status == "مستلمة") throw new InvalidOperationException("تم استلام هذه الفاتورة من قبل؛ لن يتم تكرار زيادة المخزون.");
        if (purchase.Details.Count == 0) throw new InvalidOperationException("الفاتورة القديمة لا تحتوي على تفاصيل أصناف.");
        foreach (var line in purchase.Details)
        {
            var product = Products.First(x => x.Code == line.ProductCode);
            var oldStock = product.Stock;
            var newStock = oldStock + line.Quantity;
            product.Cost = newStock > 0 ? ((oldStock * product.Cost) + (line.Total)) / newStock : line.UnitCost;
            product.Stock = newStock;
            Movements.Add(new StockMovement { Date = DateTime.Now, ProductCode = product.Code, ProductName = product.Name, Type = "وارد مشتريات", Quantity = line.Quantity, Reason = $"استلام فاتورة {purchase.Number}", User = "النظام" });
        }
        purchase.IsReceived = true; purchase.Status = "مستلمة"; purchase.MarkUpdated();
        if (save) SaveChanges();
    }

    public SupplierPayment RecordSupplierPayment(string supplierName, decimal amount, string method, string note)
        => RecordSupplierPayment(supplierName, amount, method, note, null);

    public SupplierPayment RecordSupplierPayment(string supplierName, decimal amount, string method, string note, SupplierInstallment? installment)
    {
        var supplier = Suppliers.FirstOrDefault(x => x.Name == supplierName) ?? throw new InvalidOperationException("المورد غير موجود.");
        if (amount <= 0 || amount > supplier.Balance) throw new InvalidOperationException("أدخل مبلغًا أكبر من صفر ولا يتجاوز رصيد المورد المستحق.");
        if (installment is not null && (!SupplierInstallments.Contains(installment) || installment.SupplierName != supplier.Name || amount > installment.Remaining)) throw new InvalidOperationException("قيمة الدفعة تتجاوز المتبقي على القسط المحدد.");
        var before = supplier.Balance; supplier.Balance -= amount;
        var payment = new SupplierPayment { ReceiptNumber = "SP-" + (ErpDataStore.MaxSuffix(SupplierPayments.Select(x => x.ReceiptNumber), "SP-") + 1).ToString("0000"), Date = DateTime.Now, SupplierName = supplier.Name, PurchaseNumber = installment?.PurchaseNumber ?? "", InstallmentId = installment?.Id ?? "", Amount = amount, Method = string.IsNullOrWhiteSpace(method) ? "نقدي" : method, Note = note ?? "", BalanceBefore = before, BalanceAfter = supplier.Balance };
        SupplierPayments.Add(payment);
        if (installment is not null)
        {
            installment.PaidAmount += amount;
            if (installment.Remaining == 0) installment.PaidDate = DateTime.Now;
            if (Purchases.FirstOrDefault(p => p.Number == installment.PurchaseNumber) is { } purchase) purchase.Paid += amount;
        }
        SaveChanges(); return payment;
    }

    public void SaveSupplier(Supplier? supplier, string code, string name, string phone, string address, string notes)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("كود المورد والاسم مطلوبان.");
        if (Suppliers.Any(x => !ReferenceEquals(x, supplier) && x.Code.Equals(code.Trim(), StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("كود المورد مستخدم بالفعل.");
        if (!string.IsNullOrWhiteSpace(phone) && phone.Any(c => !char.IsDigit(c) && c != '+' && c != '-' && c != ' ')) throw new InvalidOperationException("رقم الهاتف غير صحيح.");
        if (supplier is not null && !supplier.Name.Equals(name.Trim(), StringComparison.Ordinal) && (Purchases.Any(x => x.Supplier == supplier.Name) || SupplierPayments.Any(x => x.SupplierName == supplier.Name) || SupplierInstallments.Any(x => x.SupplierName == supplier.Name))) throw new InvalidOperationException("لا يمكن تغيير اسم المورد بعد وجود حركات مالية مرتبطة به.");
        supplier ??= new Supplier();
        if (!Suppliers.Contains(supplier)) Suppliers.Add(supplier);
        supplier.Code = code.Trim(); supplier.Name = name.Trim(); supplier.Phone = phone.Trim(); supplier.Address = address.Trim(); supplier.Notes = notes.Trim(); supplier.IsActive = true;
        SaveChanges();
    }

    public bool RemoveOrArchiveSupplier(Supplier supplier)
    {
        bool hasHistory = Purchases.Any(x => x.Supplier == supplier.Name) || SupplierPayments.Any(x => x.SupplierName == supplier.Name) || SupplierInstallments.Any(x => x.SupplierName == supplier.Name) || supplier.Balance != 0;
        if (hasHistory) supplier.IsActive = false; else Suppliers.Remove(supplier);
        SaveChanges(); return hasHistory;
    }

    /// <summary>Persist all operational collections atomically to the shared SQLite database.</summary>
    public void SaveChanges() => ErpDataStore.Save(this, dataStorePath);

    public (decimal NetProfit, decimal Sales, decimal CostOfGoods, decimal Expenses, bool HasCompleteCostSnapshot) CalculateNetProfit(DateTime from, DateTime to)
    {
        var invoices = Invoices.Where(x => x.Date >= from && x.Date <= to).ToList();
        var sales = invoices.Sum(x => x.Total);
        var lines = invoices.SelectMany(x => x.Lines).ToList();
        var completeCosts = lines.All(x => x.CostAtSale.HasValue);
        var cogs = lines.Sum(line => line.Quantity * (line.CostAtSale ?? Products.FirstOrDefault(p => p.Code == line.ProductCode)?.Cost ?? 0));
        var expenses = Expenses.Where(x => x.Date >= from && x.Date <= to).Sum(x => x.Amount);
        return (sales - cogs - expenses, sales, cogs, expenses, completeCosts);
    }

    public void SavePartner(Partner? partner, string code, string fullName, string nationalId, string mobile, string alternatePhone, string address, string email, DateTime joinDate, decimal initialCapital, DateTime contributionDate, decimal profitShare, decimal lossShare, decimal capitalOwnership, bool lossesReduceCapitalAccount, string notes)
    {
        code = code.Trim(); fullName = fullName.Trim(); nationalId = nationalId.Trim();
        if (code.Length == 0 || fullName.Length == 0) throw new InvalidOperationException("كود الشريك والاسم الكامل مطلوبان.");
        if (Partners.Any(x => !ReferenceEquals(x, partner) && x.Code.Equals(code, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("كود الشريك مستخدم بالفعل.");
        if (joinDate.Date > DateTime.Today.AddDays(1) || contributionDate.Date < joinDate.Date) throw new InvalidOperationException("راجع تاريخ الانضمام وتاريخ المساهمة.");
        if (initialCapital < 0 || profitShare < 0 || profitShare > 100 || lossShare < 0 || lossShare > 100 || capitalOwnership < 0 || capitalOwnership > 100) throw new InvalidOperationException("تحقق من قيمة رأس المال ونسب الربح والخسارة (من 0 إلى 100%).");
        if (!string.IsNullOrWhiteSpace(nationalId) && (nationalId.Length != 14 || nationalId.Any(c => !char.IsDigit(c)))) throw new InvalidOperationException("الرقم القومي المصري إن تم إدخاله يجب أن يتكون من 14 رقماً.");
        if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@')) throw new InvalidOperationException("البريد الإلكتروني غير صحيح.");
        if (partner is null)
        {
            partner = new Partner { Code = code, FullName = fullName, NationalId = nationalId, Mobile = mobile.Trim(), AlternatePhone = alternatePhone.Trim(), Address = address.Trim(), Email = email.Trim(), JoinDate = joinDate.Date, InitialCapital = initialCapital, InitialContributionDate = contributionDate.Date, ProfitSharePercent = profitShare, LossSharePercent = lossShare, LossesReduceCapitalAccount = lossesReduceCapitalAccount, CapitalOwnershipPercent = capitalOwnership, Notes = notes.Trim() };
            Partners.Add(partner);
            if (initialCapital > 0) PartnerTransactions.Add(new PartnerTransaction { PartnerCode = code, Type = "Contribution", Date = contributionDate.Date, Amount = initialCapital, ReferenceNumber = "INITIAL-" + code, Notes = "مساهمة رأس المال الأولية" });
        }
        else
        {
            if (partner.Code != code && PartnerTransactions.Any(t => t.PartnerCode == partner.Code)) throw new InvalidOperationException("لا يمكن تغيير كود شريك له حركات مالية.");
            partner.Code = code; partner.FullName = fullName; partner.NationalId = nationalId; partner.Mobile = mobile.Trim(); partner.AlternatePhone = alternatePhone.Trim(); partner.Address = address.Trim(); partner.Email = email.Trim(); partner.JoinDate = joinDate.Date; partner.ProfitSharePercent = profitShare; partner.LossSharePercent = lossShare; partner.LossesReduceCapitalAccount = lossesReduceCapitalAccount; partner.CapitalOwnershipPercent = capitalOwnership; partner.Notes = notes.Trim();
        }
        SaveChanges();
    }

    public PartnerTransaction RecordPartnerCapitalTransaction(string partnerCode, bool contribution, DateTime date, decimal amount, string method, string reference, string notes)
    {
        var p = Partners.FirstOrDefault(x => x.Code == partnerCode && x.IsActive) ?? throw new InvalidOperationException("الشريك غير موجود أو غير نشط.");
        if (amount <= 0 || date.Date < p.JoinDate.Date || p.ExitDate is not null && date.Date > p.ExitDate.Value.Date) throw new InvalidOperationException("تحقق من تاريخ العملية وقيمة موجبة.");
        var capital = PartnerTransactions.Where(x => x.PartnerCode == partnerCode && x.Type == "Contribution").Sum(x => x.Amount) - PartnerTransactions.Where(x => x.PartnerCode == partnerCode && x.Type == "CapitalReturn").Sum(x => x.Amount);
        if (!contribution && amount > capital) throw new InvalidOperationException("قيمة رد رأس المال تتجاوز رأس المال المتبقي.");
        var transaction = new PartnerTransaction { PartnerCode = partnerCode, Type = contribution ? "Contribution" : "CapitalReturn", Date = date.Date, Amount = amount, PaymentMethod = method, ReferenceNumber = reference.Trim(), Notes = notes.Trim() };
        PartnerTransactions.Add(transaction); SaveChanges(); return transaction;
    }

    public decimal PartnerCapitalRemaining(string partnerCode) => PartnerTransactions.Where(x => x.PartnerCode == partnerCode && x.Type == "Contribution").Sum(x => x.Amount) - PartnerTransactions.Where(x => x.PartnerCode == partnerCode && x.Type == "CapitalReturn").Sum(x => x.Amount);
    public decimal PartnerProfitEarned(string partnerCode) => PartnerAllocations.Where(x => x.PartnerCode == partnerCode && x.IsFinalized).Sum(x => x.AllocatedProfit) + PartnerTransactions.Where(x => x.PartnerCode == partnerCode && x.Type == "ProfitAdjustment").Sum(x => x.Amount);
    public decimal PartnerLossAllocated(string partnerCode) => PartnerAllocations.Where(x => x.PartnerCode == partnerCode && x.IsFinalized).Sum(x => x.AllocatedLoss);
    public decimal PartnerProfitPaid(string partnerCode) => PartnerTransactions.Where(x => x.PartnerCode == partnerCode && x.Type == "ProfitDistribution").Sum(x => x.Amount);

    public PartnerTransaction RecordPartnerProfitAdjustment(string partnerCode, DateTime date, decimal amount, string reference, string reason)
    {
        if (!Partners.Any(x => x.Code == partnerCode && x.IsActive) || amount == 0 || string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("اختر شريكاً نشطاً وأدخل تسوية أرباح غير صفرية وسبباً واضحاً.");
        var transaction = new PartnerTransaction { PartnerCode = partnerCode, Type = "ProfitAdjustment", Date = date.Date, Amount = amount, ReferenceNumber = reference.Trim(), Notes = reason.Trim() };
        PartnerTransactions.Add(transaction); SaveChanges(); return transaction;
    }

    public IReadOnlyList<PartnerProfitAllocation> CalculatePartnerMonth(int year, int month)
    {
        var start = new DateTime(year, month, 1); var end = start.AddMonths(1).AddTicks(-1);
        var eligible = Partners.Where(p => p.IsActive && !PartnerSettlements.Any(s => s.PartnerCode == p.Code) && p.JoinDate.Date <= start && (p.ExitDate is null || p.ExitDate.Value.Date >= end.Date)).ToList();
        if (eligible.Count == 0) throw new InvalidOperationException("لا يوجد شركاء مستحقون عن شهر كامل في هذه الفترة.");
        var result = CalculateNetProfit(start, end);
        if (PartnerAllocations.Any(x => x.Year == year && x.Month == month)) return PartnerAllocations.Where(x => x.Year == year && x.Month == month).ToList();
        var positive = Math.Max(0, result.NetProfit); var loss = Math.Max(0, -result.NetProfit);
        var allocations = eligible.OrderBy(x => x.Code).Select(p => new PartnerProfitAllocation { PartnerCode = p.Code, Year = year, Month = month, BusinessNetProfit = result.NetProfit, EffectiveSharePercent = p.ProfitSharePercent, EffectiveLossSharePercent = p.LossSharePercent, LossReducesCapitalAccount = p.LossesReduceCapitalAccount, AllocatedProfit = decimal.Round(positive * p.ProfitSharePercent / 100m, 2, MidpointRounding.AwayFromZero), AllocatedLoss = decimal.Round(loss * p.LossSharePercent / 100m, 2, MidpointRounding.AwayFromZero), IsFinalized = false }).ToList();
        return allocations;
    }

    public IReadOnlyList<PartnerProfitAllocation> FinalizePartnerMonth(int year, int month)
    {
        var start = new DateTime(year, month, 1); var end = start.AddMonths(1).AddTicks(-1);
        if (!CalculateNetProfit(start, end).HasCompleteCostSnapshot) throw new InvalidOperationException("لا يمكن اعتماد التوزيع: تكلفة البضاعة التاريخية غير مكتملة لهذه الفترة.");
        if (PartnerAllocations.Any(x => x.Year == year && x.Month == month)) throw new InvalidOperationException("توجد سجلات لهذه الفترة بالفعل؛ لا يمكن إعادة اعتماد الشهر.");
        var eligible = Partners.Where(p => p.IsActive && !PartnerSettlements.Any(s => s.PartnerCode == p.Code) && p.JoinDate.Date <= start && (p.ExitDate is null || p.ExitDate.Value.Date >= end.Date)).ToList();
        var businessResult = CalculateNetProfit(start, end).NetProfit;
        var requiredShare = businessResult > 0 ? eligible.Sum(x => x.ProfitSharePercent) : businessResult < 0 ? eligible.Sum(x => x.LossSharePercent) : 100m;
        if (businessResult != 0 && requiredShare > 100m) throw new InvalidOperationException(businessResult > 0 ? "مجموع نسب توزيع الأرباح يتجاوز 100%؛ راجع الاتفاق قبل الاعتماد." : "مجموع نسب تحمل الخسائر يتجاوز 100%؛ راجع الاتفاق قبل الاعتماد.");
        var drafts = CalculatePartnerMonth(year, month).ToList();
        if (drafts.Any(x => PartnerAllocations.Any(a => a.PartnerCode == x.PartnerCode && a.Year == year && a.Month == month && a.IsFinalized))) throw new InvalidOperationException("يوجد توزيع نهائي مسجل بالفعل لهذه الفترة.");
        if (drafts.Count > 0 && drafts.Sum(x => x.BusinessNetProfit) != drafts[0].BusinessNetProfit) throw new InvalidOperationException("قاعدة الربح غير متسقة بين الشركاء.");
        if (drafts.Count > 0 && drafts[0].BusinessNetProfit > 0 && eligible.Sum(x => x.ProfitSharePercent) == 100m) drafts[^1].AllocatedProfit += drafts[0].BusinessNetProfit - drafts.Sum(x => x.AllocatedProfit);
        if (drafts.Count > 0 && drafts[0].BusinessNetProfit < 0 && eligible.Sum(x => x.LossSharePercent) == 100m) drafts[^1].AllocatedLoss += -drafts[0].BusinessNetProfit - drafts.Sum(x => x.AllocatedLoss);
        foreach (var allocation in drafts) { allocation.IsFinalized = true; PartnerAllocations.Add(allocation); }
        SaveChanges(); return drafts;
    }

    public PartnerTransaction ReversePartnerTransaction(string transactionId, DateTime date, string reason)
    {
        var original = PartnerTransactions.FirstOrDefault(x => x.Id == transactionId) ?? throw new InvalidOperationException("الحركة المطلوب تصحيحها غير موجودة.");
        if (!string.IsNullOrWhiteSpace(original.SettlementId)) throw new InvalidOperationException("حركات تسوية الخروج تُصحح من سجل التسوية نفسه.");
        if (PartnerTransactions.Any(x => x.ReversesTransactionId == transactionId)) throw new InvalidOperationException("تم عكس هذه الحركة بالفعل.");
        if (original.Type is not ("Contribution" or "CapitalReturn" or "ProfitDistribution" or "ProfitAdjustment")) throw new InvalidOperationException("نوع الحركة لا يدعم العكس.");
        if (original.Type == "Contribution" && PartnerCapitalRemaining(original.PartnerCode) < original.Amount) throw new InvalidOperationException("لا يمكن عكس مساهمة سبق رد جزء من رأس مالها.");
        var reversal = new PartnerTransaction { PartnerCode = original.PartnerCode, Type = original.Type, Date = date.Date, Amount = -original.Amount, PaymentMethod = original.PaymentMethod, ReferenceNumber = "REV-" + original.ReferenceNumber, Notes = reason.Trim(), ReversesTransactionId = original.Id };
        PartnerTransactions.Add(reversal); SaveChanges(); return reversal;
    }

    public PartnerTransaction RecordPartnerProfitDistribution(string partnerCode, DateTime date, decimal amount, string method, string reference, string notes)
    {
        if (!Partners.Any(x => x.Code == partnerCode && x.IsActive)) throw new InvalidOperationException("صرف الأرباح المباشر متاح للشريك النشط فقط؛ استخدم سداد التسوية للشريك الخارج.");
        var unpaid = PartnerProfitEarned(partnerCode) - PartnerProfitPaid(partnerCode);
        if (amount <= 0 || amount > unpaid) throw new InvalidOperationException("المبلغ يجب أن يكون موجباً ولا يتجاوز الأرباح المعتمدة غير المدفوعة.");
        var transaction = new PartnerTransaction { PartnerCode = partnerCode, Type = "ProfitDistribution", Date = date.Date, Amount = amount, PaymentMethod = method, ReferenceNumber = reference.Trim(), Notes = notes.Trim() };
        PartnerTransactions.Add(transaction); SaveChanges(); return transaction;
    }

    public PartnerExitSettlement RecordPartnerSettlementPayment(string settlementId, DateTime date, decimal amount, string method, string reference, string notes)
    {
        var settlement = PartnerSettlements.FirstOrDefault(x => x.Id == settlementId) ?? throw new InvalidOperationException("تسوية الشريك غير موجودة.");
        if (amount <= 0 || amount > settlement.RemainingSettlement) throw new InvalidOperationException("قيمة الدفعة يجب أن تكون موجبة ولا تتجاوز المتبقي من التسوية.");
        var capitalOutstanding = Math.Max(0, settlement.RemainingCapital - settlement.CapitalPaid);
        var capitalPayment = Math.Min(capitalOutstanding, amount); var profitPayment = amount - capitalPayment;
        if (profitPayment > settlement.UnpaidProfit - settlement.ProfitPaid) throw new InvalidOperationException("قيمة الدفعة تتجاوز الأرباح المستحقة بالتسوية.");
        var partnerCode = settlement.PartnerCode;
        if (capitalPayment > 0) PartnerTransactions.Add(new PartnerTransaction { PartnerCode = partnerCode, SettlementId = settlement.Id, Type = "CapitalReturn", Date = date.Date, Amount = capitalPayment, PaymentMethod = method, ReferenceNumber = reference, Notes = "دفعة لاحقة من تسوية الخروج - رأس المال" });
        if (profitPayment > 0) PartnerTransactions.Add(new PartnerTransaction { PartnerCode = partnerCode, SettlementId = settlement.Id, Type = "ProfitDistribution", Date = date.Date, Amount = profitPayment, PaymentMethod = method, ReferenceNumber = reference, Notes = "دفعة لاحقة من تسوية الخروج - الأرباح" });
        settlement.CapitalPaid += capitalPayment; settlement.ProfitPaid += profitPayment; settlement.AmountPaid += amount; settlement.Notes = string.IsNullOrWhiteSpace(notes) ? settlement.Notes : settlement.Notes + " | " + notes.Trim();
        SaveChanges(); return settlement;
    }

    public PartnerExitSettlement PreviewPartnerExit(string partnerCode, DateTime exitDate, decimal approvedDeductions)
    {
        var partner = Partners.FirstOrDefault(x => x.Code == partnerCode && x.IsActive) ?? throw new InvalidOperationException("الشريك غير موجود أو خرج بالفعل.");
        if (exitDate.Date < partner.JoinDate.Date || approvedDeductions < 0) throw new InvalidOperationException("تاريخ الخروج أو الخصومات غير صحيحة.");
        if (PartnerSettlements.Any(x => x.PartnerCode == partnerCode)) throw new InvalidOperationException("تم تسجيل تسوية خروج لهذا الشريك بالفعل.");
        var contributed = PartnerTransactions.Where(x => x.PartnerCode == partnerCode && x.Type == "Contribution" && x.Date.Date <= exitDate.Date).Sum(x => x.Amount);
        var capitalReturned = PartnerTransactions.Where(x => x.PartnerCode == partnerCode && x.Type == "CapitalReturn" && x.Date.Date <= exitDate.Date).Sum(x => x.Amount);
        var capital = contributed - capitalReturned;
        var earnedByExit = PartnerAllocations.Where(x => x.PartnerCode == partnerCode && x.IsFinalized && new DateTime(x.Year, x.Month, 1).AddMonths(1).AddDays(-1) <= exitDate.Date).Sum(x => x.AllocatedProfit)
            + PartnerTransactions.Where(x => x.PartnerCode == partnerCode && x.Type == "ProfitAdjustment" && x.Date <= exitDate.Date).Sum(x => x.Amount);
        var paidByExit = PartnerTransactions.Where(x => x.PartnerCode == partnerCode && x.Type == "ProfitDistribution" && x.Date <= exitDate.Date).Sum(x => x.Amount);
        var unpaidProfit = earnedByExit - paidByExit;
        var lossAllocationsByExit = PartnerAllocations.Where(x => x.PartnerCode == partnerCode && x.IsFinalized && new DateTime(x.Year, x.Month, 1).AddMonths(1).AddDays(-1) <= exitDate.Date).ToList();
        var lossesByExit = lossAllocationsByExit.Sum(x => x.AllocatedLoss);
        var lossOffset = Math.Min(capital, lossAllocationsByExit.Where(x => x.LossReducesCapitalAccount).Sum(x => x.AllocatedLoss));
        var settlementCapital = Math.Max(0, capital - lossOffset);
        var proposed = Math.Max(0, settlementCapital + unpaidProfit - approvedDeductions);
        return new PartnerExitSettlement { PartnerCode = partnerCode, ProposedExitDate = exitDate.Date, RemainingCapital = settlementCapital, TotalCapitalContributed = contributed, CapitalReturnedPreviously = capitalReturned, AccumulatedProfits = earnedByExit, AccumulatedLosses = lossesByExit, ProfitAlreadyPaid = paidByExit, CapitalLossOffset = lossOffset, UnpaidProfit = unpaidProfit, ApprovedDeductions = approvedDeductions, ProposedTotal = proposed };
    }

    public PartnerExitSettlement ConfirmPartnerExit(string partnerCode, DateTime exitDate, decimal deductions, decimal amountPaid, string method, string reference, string notes)
    {
        var settlement = PreviewPartnerExit(partnerCode, exitDate, deductions);
        if (amountPaid < 0 || amountPaid > settlement.ProposedTotal) throw new InvalidOperationException("المبلغ المدفوع يجب أن يكون بين صفر وإجمالي التسوية المقترحة.");
        settlement.AmountPaid = amountPaid;
        settlement.CapitalPaid = Math.Min(amountPaid, settlement.RemainingCapital);
        settlement.ProfitPaid = Math.Max(0, amountPaid - settlement.CapitalPaid);
        settlement.ReferenceNumber = reference.Trim(); settlement.Notes = notes.Trim();
        if (settlement.CapitalPaid > 0) PartnerTransactions.Add(new PartnerTransaction { PartnerCode = partnerCode, SettlementId = settlement.Id, Type = "CapitalReturn", Date = exitDate.Date, Amount = settlement.CapitalPaid, PaymentMethod = method, ReferenceNumber = reference, Notes = "جزء رأس المال المدفوع عند الخروج" });
        if (settlement.ProfitPaid > 0) PartnerTransactions.Add(new PartnerTransaction { PartnerCode = partnerCode, SettlementId = settlement.Id, Type = "ProfitDistribution", Date = exitDate.Date, Amount = settlement.ProfitPaid, PaymentMethod = method, ReferenceNumber = reference, Notes = "جزء الأرباح المدفوع عند الخروج" });
        PartnerSettlements.Add(settlement);
        var partner = Partners.First(x => x.Code == partnerCode); partner.IsActive = false; partner.ExitDate = exitDate.Date;
        SaveChanges(); return settlement;
    }

    public void SaveTrader(Customer? trader, string code, string name, string phone, string address)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("الكود والاسم مطلوبان.");
        if (Customers.Any(x => !ReferenceEquals(x, trader) && x.Code.Equals(code.Trim(), StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("الكود مستخدم بالفعل.");
        if (!string.IsNullOrWhiteSpace(phone) && phone.Any(c => !char.IsDigit(c) && c != '+' && c != '-' && c != ' ')) throw new InvalidOperationException("رقم الهاتف غير صحيح.");
        if (trader is null)
        {
            trader = new Customer { Code = code.Trim(), Type = "تاجر" };
            Customers.Add(trader);
        }
        if (!trader.Type.Equals("تاجر", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("السجل المحدد ليس تاجراً.");
        trader.Code = code.Trim(); trader.Name = name.Trim(); trader.Phone = phone.Trim(); trader.Address = address.Trim(); trader.IsActive = true;
        SaveChanges();
    }

    public void SaveRepresentative(Representative? representative, string code, string name, string phone, string address, string notes)
    {
        code = code.Trim();
        name = name.Trim();
        phone = phone.Trim();
        address = address.Trim();
        notes = notes.Trim();
        if (code.Length == 0 || name.Length == 0)
            throw new InvalidOperationException("كود المندوب والاسم مطلوبان.");
        if (Representatives.Any(x => !ReferenceEquals(x, representative) && x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("كود المندوب مستخدم بالفعل.");
        if (phone.Any(c => !char.IsDigit(c) && c != '+' && c != '-' && c != ' '))
            throw new InvalidOperationException("رقم الهاتف غير صحيح.");
        if (representative is not null && !representative.Code.Equals(code, StringComparison.OrdinalIgnoreCase) &&
            (Invoices.Any(x => x.RepresentativeCode.Equals(representative.Code, StringComparison.OrdinalIgnoreCase)) ||
             CommissionRules.Any(x => x.RepresentativeCode.Equals(representative.Code, StringComparison.OrdinalIgnoreCase))))
            throw new InvalidOperationException("لا يمكن تغيير كود مندوب مرتبط بفواتير أو قواعد عمولة محفوظة.");

        var isNew = representative is null;
        representative ??= new Representative();
        var previous = (representative.Code, representative.Name, representative.Phone, representative.Address, representative.Notes, representative.IsActive);
        representative.Code = code;
        representative.Name = name;
        representative.Phone = phone;
        representative.Address = address;
        representative.Notes = notes;
        if (isNew) representative.IsActive = true;
        if (isNew) Representatives.Add(representative);
        try
        {
            SaveChanges();
        }
        catch
        {
            if (isNew) Representatives.Remove(representative);
            else
            {
                representative.Code = previous.Code;
                representative.Name = previous.Name;
                representative.Phone = previous.Phone;
                representative.Address = previous.Address;
                representative.Notes = previous.Notes;
                representative.IsActive = previous.IsActive;
            }
            throw;
        }
    }

    public void SetRepresentativeActive(Representative representative, bool isActive)
    {
        ArgumentNullException.ThrowIfNull(representative);
        if (!Representatives.Contains(representative))
            throw new InvalidOperationException("المندوب المحدد غير موجود.");
        var previous = representative.IsActive;
        representative.IsActive = isActive;
        try
        {
            SaveChanges();
        }
        catch
        {
            representative.IsActive = previous;
            throw;
        }
    }

    public bool RepresentativeHasHistory(Representative representative) => Invoices.Any(x =>
        x.RepresentativeCode.Equals(representative.Code, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<string> CommissionSaleTypes { get; } = new[] { "كاش", "قسط أفراد", "آجل تاجر" };

    public void SaveCommissionRule(CommissionRule? rule, string representativeCode, string description,
        string? productCode, string? saleType, decimal ratePercent, int priority, bool isActive)
    {
        representativeCode = representativeCode?.Trim() ?? "";
        description = description?.Trim() ?? "";
        productCode = productCode?.Trim() ?? "";
        saleType = saleType?.Trim() ?? "";
        if (representativeCode.Length == 0 || description.Length == 0)
            throw new InvalidOperationException("يجب تحديد المندوب ووصف القاعدة.");
        if (ratePercent < 0)
            throw new InvalidOperationException("نسبة العمولة لا يمكن أن تكون سالبة.");
        if (priority < 0)
            throw new InvalidOperationException("الأولوية يجب أن تكون صفراً أو أكبر.");
        if (productCode.Length > 0 && !Products.Any(p => p.Code.Equals(productCode, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("الصنف المحدد غير موجود.");
        if (saleType.Length > 0 && !CommissionSaleTypes.Contains(saleType, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("نوع البيع المحدد غير مدعوم.");

        var existing = rule is null ? null : CommissionRules.FirstOrDefault(x => ReferenceEquals(x, rule));
        if (rule is not null && existing is null)
            throw new InvalidOperationException("قاعدة العمولة المحددة غير موجودة.");
        var representative = Representatives.FirstOrDefault(x => x.Code.Equals(representativeCode, StringComparison.OrdinalIgnoreCase));
        if (existing is null && (representative is null || !representative.IsActive))
            throw new InvalidOperationException("يجب اختيار مندوب نشط لإنشاء قاعدة جديدة.");
        if (existing is not null && !existing.RepresentativeCode.Equals(representativeCode, StringComparison.OrdinalIgnoreCase) &&
            (representative is null || !representative.IsActive))
            throw new InvalidOperationException("لا يمكن نقل القاعدة إلى مندوب غير نشط أو غير موجود.");
        if (existing?.Basis is not CommissionBasis.LineValueAfterLineDiscount && existing is not null)
            throw new InvalidOperationException("أساس العمولة المخزن غير مدعوم ولا يمكن تغييره ضمن هذه المرحلة.");

        if (CommissionRules.Any(x => !ReferenceEquals(x, existing) &&
            x.RepresentativeCode.Equals(representativeCode, StringComparison.OrdinalIgnoreCase) &&
            x.ProductCode.Equals(productCode, StringComparison.OrdinalIgnoreCase) &&
            x.SaleType.Equals(saleType, StringComparison.OrdinalIgnoreCase) && x.Priority == priority))
            throw new InvalidOperationException("توجد قاعدة بنفس المندوب والنطاق والأولوية بالفعل.");

        var target = existing ?? new CommissionRule();
        var previous = existing is null ? null : new CommissionRule
        {
            Id = target.Id, RepresentativeCode = target.RepresentativeCode, Description = target.Description,
            ProductCode = target.ProductCode, SaleType = target.SaleType, RatePercent = target.RatePercent,
            Basis = target.Basis, Priority = target.Priority, IsActive = target.IsActive
        };
        target.RepresentativeCode = representativeCode;
        target.Description = description;
        target.ProductCode = productCode;
        target.SaleType = saleType;
        target.RatePercent = ratePercent;
        target.Basis = CommissionBasis.LineValueAfterLineDiscount;
        target.Priority = priority;
        target.IsActive = isActive;
        if (existing is null) CommissionRules.Add(target);
        try { SaveChanges(); }
        catch
        {
            if (existing is null) CommissionRules.Remove(target);
            else
            {
                target.RepresentativeCode = previous!.RepresentativeCode;
                target.Description = previous.Description;
                target.ProductCode = previous.ProductCode;
                target.SaleType = previous.SaleType;
                target.RatePercent = previous.RatePercent;
                target.Basis = previous.Basis;
                target.Priority = previous.Priority;
                target.IsActive = previous.IsActive;
            }
            throw;
        }
    }

    public void SetCommissionRuleActive(CommissionRule rule, bool isActive)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (!CommissionRules.Contains(rule)) throw new InvalidOperationException("قاعدة العمولة المحددة غير موجودة.");
        var previous = rule.IsActive;
        rule.IsActive = isActive;
        try { SaveChanges(); }
        catch { rule.IsActive = previous; throw; }
    }

    /// <summary>Resolves a configured rule for future use; this does not calculate or accrue commission.</summary>
    public CommissionRule? ResolveCommissionRule(string representativeCode, string productCode, string saleType) =>
        CommissionRules.Where(rule => rule.IsActive && rule.RepresentativeCode.Equals(representativeCode, StringComparison.OrdinalIgnoreCase) &&
                (rule.ProductCode.Length == 0 || rule.ProductCode.Equals(productCode, StringComparison.OrdinalIgnoreCase)) &&
                (rule.SaleType.Length == 0 || rule.SaleType.Equals(saleType, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(rule => rule.ProductCode.Length > 0 && rule.SaleType.Length > 0 ? 0 : rule.ProductCode.Length > 0 ? 1 : rule.SaleType.Length > 0 ? 2 : 3)
            .ThenBy(rule => rule.Priority)
            .ThenBy(rule => rule.Id, StringComparer.Ordinal)
            .FirstOrDefault();

    private List<CommissionAccrual> CreateCommissionAccruals(Payment payment)
    {
        var result = new List<CommissionAccrual>();
        var processedAllocationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var allocation in payment.Allocations)
        {
            if (string.IsNullOrWhiteSpace(allocation.Id) || allocation.Amount <= 0 || !processedAllocationIds.Add(allocation.Id)) continue;
            var invoice = Invoices.FirstOrDefault(x => x.Number.Equals(allocation.InvoiceNumber, StringComparison.OrdinalIgnoreCase));
            if (invoice is null || string.IsNullOrWhiteSpace(invoice.RepresentativeCode) ||
                string.IsNullOrWhiteSpace(invoice.RepresentativeName) || invoice.Lines.Count == 0) continue;

            var eligibleBase = invoice.Lines.Sum(line => line.Total);
            if (invoice.Total <= 0 || eligibleBase <= 0 || invoice.Total > eligibleBase ||
                invoice.DiscountPercent < 0 || invoice.DiscountPercent > 100 ||
                invoice.Lines.Any(line => string.IsNullOrWhiteSpace(line.Id)) ||
                invoice.Lines.Select(line => line.Id).Distinct(StringComparer.Ordinal).Count() != invoice.Lines.Count)
                continue;

            // Customer collections are linked to an invoice, not a product. The explicit
            // allocation policy is pro rata over each line's post-line-discount value.
            // Phase 3's commission basis excludes invoice-wide discounts, so a complete
            // collection maps back to each line's full eligible value.
            var oldUnlinedAccruals = CommissionAccruals.Any(x =>
                x.InvoiceNumber.Equals(invoice.Number, StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(x.InvoiceLineId));
            if (oldUnlinedAccruals) continue;

            var snapshots = invoice.Lines.Select(line => new CommissionAccrualLineSnapshot
            {
                InvoiceLineId = line.Id,
                ProductCode = line.ProductCode,
                ProductName = line.ProductName,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                LineDiscount = line.Discount,
                EligibleValue = line.Total
            }).ToList();
            foreach (var line in invoice.Lines)
            {
                var rule = ResolveCommissionRule(invoice.RepresentativeCode, line.ProductCode, invoice.Type);
                if (rule is null || rule.Basis != CommissionBasis.LineValueAfterLineDiscount || rule.RatePercent < 0) continue;

                var priorLineAccruals = CommissionAccruals.Concat(result).Where(x =>
                    x.InvoiceNumber.Equals(invoice.Number, StringComparison.OrdinalIgnoreCase) && x.InvoiceLineId == line.Id).ToList();
                if (priorLineAccruals.Any(x => x.SourceAllocationId == allocation.Id)) continue;
                var priorBasis = priorLineAccruals.Sum(x => x.CollectedEligibleAmount);
                var lineShare = decimal.Round(allocation.Amount * line.Total / invoice.Total, 2, MidpointRounding.AwayFromZero);
                var eligibleCollected = Math.Min(Math.Max(0, line.Total - priorBasis), lineShare);
                if (eligibleCollected <= 0) continue;

                var sameRateAccruals = priorLineAccruals.Where(x => x.RuleId == rule.Id && x.RatePercent == rule.RatePercent).ToList();
                var rateBasis = sameRateAccruals.Sum(x => x.CollectedEligibleAmount);
                var priorCommission = sameRateAccruals.Sum(x => x.CommissionAmount);
                var cumulativeCommission = decimal.Round((rateBasis + eligibleCollected) * rule.RatePercent / 100m, 2, MidpointRounding.AwayFromZero);
                var commission = Math.Max(0, cumulativeCommission - priorCommission);

                result.Add(new CommissionAccrual
                {
                    SourceAllocationId = allocation.Id,
                    PaymentReceiptNumber = payment.ReceiptNumber,
                    CollectedAt = payment.Date,
                    InvoiceNumber = invoice.Number,
                    InvoiceLineId = line.Id,
                    ProductCode = line.ProductCode,
                    RepresentativeCode = invoice.RepresentativeCode,
                    RepresentativeName = invoice.RepresentativeName,
                    RuleId = rule.Id,
                    RuleDescription = rule.Description,
                    RuleScopeSnapshot = $"Product={rule.ProductCode}; SaleType={rule.SaleType}; Priority={rule.Priority}",
                    RatePercent = rule.RatePercent,
                    Basis = rule.Basis,
                    InvoiceEligibleBase = line.Total,
                    InvoiceTotal = invoice.Total,
                    InvoiceDiscountPercent = invoice.DiscountPercent,
                    PaymentAllocationAmount = allocation.Amount,
                    CollectedEligibleAmount = eligibleCollected,
                    CommissionAmount = commission,
                    InvoiceLines = snapshots
                });
            }
        }
        return result;
    }

    public CommissionAccrualStatus GetCommissionAccrualStatus(CommissionAccrual accrual)
    {
        if (!CommissionAccruals.Contains(accrual)) throw new InvalidOperationException("استحقاق العمولة غير موجود.");
        if (!CommissionApprovals.Any(x => x is not null && x.AccrualId == accrual.Id)) return CommissionAccrualStatus.Earned;
        var paid = RepresentativePayouts.Where(p => p is not null).SelectMany(p => p.Allocations ?? new List<CommissionPayoutAllocation>())
            .Where(x => x is not null && x.AccrualId == accrual.Id).Sum(x => x.Amount);
        if (paid >= accrual.CommissionAmount) return CommissionAccrualStatus.Paid;
        return paid > 0 ? CommissionAccrualStatus.PartiallyPaid : CommissionAccrualStatus.Approved;
    }

    public CommissionSummary GetCommissionSummary(string representativeCode)
    {
        var accruals = CommissionAccruals.Where(x => x is not null && string.Equals(x.RepresentativeCode, representativeCode, StringComparison.OrdinalIgnoreCase)).ToList();
        var ids = accruals.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var approvals = CommissionApprovals.Where(x => x is not null && ids.Contains(x.AccrualId ?? "")).ToList();
        var approvedIds = approvals.Select(x => x.AccrualId).ToHashSet(StringComparer.Ordinal);
        var earned = accruals.Sum(x => x.CommissionAmount);
        var approved = accruals.Where(x => approvedIds.Contains(x.Id)).Sum(x => x.CommissionAmount);
        var paid = RepresentativePayouts.Where(p => p is not null).SelectMany(p => (p.Allocations ?? new List<CommissionPayoutAllocation>()).Where(a => a is not null).Select(a => (Payout: p, Allocation: a)))
            .Where(x => ids.Contains(x.Allocation.AccrualId ?? "")).Sum(x => x.Allocation.Amount);
        var receipts = RepresentativePayouts.Count(p => p is not null && string.Equals(p.RepresentativeCode, representativeCode, StringComparison.OrdinalIgnoreCase));
        return new CommissionSummary(earned, approved, earned - approved, paid, Math.Max(0, approved - paid), accruals.Count, approvals.Count, receipts);
    }

    public decimal GetAccruedCommission(string representativeCode) => GetCommissionSummary(representativeCode).Earned;

    public decimal GetApprovedCommission(string representativeCode) => GetCommissionSummary(representativeCode).Approved;

    public decimal GetPaidCommission(string representativeCode) => GetCommissionSummary(representativeCode).Paid;

    public decimal GetOutstandingCommission(string representativeCode) => GetCommissionSummary(representativeCode).Outstanding;

    public void ApproveCommissionAccruals(IEnumerable<string> accrualIds, string approvedBy)
    {
        ArgumentNullException.ThrowIfNull(accrualIds);
        approvedBy = approvedBy?.Trim() ?? "";
        if (approvedBy.Length == 0) throw new InvalidOperationException("اسم المسؤول عن الاعتماد مطلوب.");
        var ids = accrualIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToList();
        if (ids.Count == 0) throw new InvalidOperationException("حدد استحقاق عمولة واحداً على الأقل.");
        var selected = ids.Select(id => CommissionAccruals.FirstOrDefault(x => x.Id == id) ?? throw new InvalidOperationException("لم يعد استحقاق العمولة المحدد موجوداً.")).ToList();
        if (selected.Any(x => x.CommissionAmount <= 0 || CommissionApprovals.Any(a => a.AccrualId == x.Id)))
            throw new InvalidOperationException("يمكن اعتماد الاستحقاقات غير المعتمدة ذات قيمة العمولة الموجبة فقط.");
        var created = selected.Select(x => new CommissionApproval { AccrualId = x.Id, Date = DateTime.Now, ApprovedBy = approvedBy }).ToList();
        CommissionApprovals.AddRange(created);
        try { SaveChanges(); }
        catch { foreach (var approval in created) CommissionApprovals.Remove(approval); throw; }
    }

    public RepresentativePayout RecordRepresentativePayout(string representativeCode, decimal amount, string method, string notes)
    {
        representativeCode = representativeCode?.Trim() ?? "";
        method = method?.Trim() ?? "";
        notes = notes?.Trim() ?? "";
        amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (representativeCode.Length == 0 || method.Length == 0 || amount <= 0)
            throw new InvalidOperationException("المندوب ومبلغ الصرف الموجب وطريقة الدفع مطلوبة.");
        var outstanding = GetOutstandingCommission(representativeCode);
        if (amount > outstanding) throw new InvalidOperationException($"مبلغ الصرف يتجاوز العمولات المعتمدة المتبقية للصرف ({outstanding:N2}).");

        var remaining = amount;
        var allocations = new List<CommissionPayoutAllocation>();
        var eligibleAccruals = CommissionAccruals
            .Where(x => x.RepresentativeCode.Equals(representativeCode, StringComparison.OrdinalIgnoreCase) && CommissionApprovals.Any(a => a.AccrualId == x.Id))
            .OrderBy(x => x.CollectedAt).ThenBy(x => x.Id, StringComparer.Ordinal).ToList();
        foreach (var accrual in eligibleAccruals)
        {
            if (remaining <= 0) break;
            var paid = RepresentativePayouts.SelectMany(x => x.Allocations).Where(x => x.AccrualId == accrual.Id).Sum(x => x.Amount);
            var open = Math.Max(0, accrual.CommissionAmount - paid);
            var applied = Math.Min(remaining, open);
            if (applied <= 0) continue;
            allocations.Add(new CommissionPayoutAllocation { AccrualId = accrual.Id, Amount = applied });
            remaining = decimal.Round(remaining - applied, 2, MidpointRounding.AwayFromZero);
        }
        if (remaining != 0) throw new InvalidOperationException("تعذر توزيع مبلغ الصرف بالكامل على استحقاقات العمولات المعتمدة.");

        var representative = Representatives.FirstOrDefault(x => x.Code.Equals(representativeCode, StringComparison.OrdinalIgnoreCase));
        var name = representative?.Name ?? CommissionAccruals.First(x => x.RepresentativeCode.Equals(representativeCode, StringComparison.OrdinalIgnoreCase)).RepresentativeName;
        var receipt = "RCP-" + (ErpDataStore.MaxSuffix(RepresentativePayouts.Select(x => x.ReceiptNumber), "RCP-") + 1).ToString("000000");
        var payout = new RepresentativePayout { ReceiptNumber = receipt, RepresentativeCode = representativeCode, RepresentativeName = name, Amount = amount, Method = method, Notes = notes, Allocations = allocations };
        RepresentativePayouts.Add(payout);
        try { SaveChanges(); }
        catch { RepresentativePayouts.Remove(payout); throw; }
        return payout;
    }

    public bool RemoveOrArchiveTrader(Customer trader)
    {
        if (!trader.Type.Equals("تاجر", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("لا يمكن حذف سجل عميل من إدارة التجار.");
        bool hasHistory = Invoices.Any(x => x.CustomerName.Equals(trader.Name, StringComparison.OrdinalIgnoreCase)) || Payments.Any(x => x.CustomerName.Equals(trader.Name, StringComparison.OrdinalIgnoreCase)) || Installments.Any(x => x.CustomerName.Equals(trader.Name, StringComparison.OrdinalIgnoreCase));
        if (hasHistory) trader.IsActive = false; else Customers.Remove(trader);
        SaveChanges();
        return hasHistory;
    }
    public IReadOnlyList<Product> LowStockProducts => Products
        .Where(p => p.Stock <= p.MinimumStock)
        .OrderBy(p => p.Stock)
        .ToList();

    public void AddProduct(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        ValidateProduct(product, null);
        product.Code = NormalizeProductCode(product.Code);
        product.Barcode = product.Barcode.Trim();
        product.Name = product.Name.Trim();
        product.Category = product.Category.Trim();
        Products.Add(product);
        if (product.Stock > 0)
        {
            Movements.Insert(0, new StockMovement
            {
                ProductCode = product.Code,
                ProductName = product.Name,
                Type = "رصيد افتتاحي",
                Quantity = product.Stock,
                Reason = "رصيد افتتاحي عند إضافة الصنف"
            });
        }
        SaveChanges();
    }

    public void UpdateProduct(string originalCode, Product updated)
    {
        ArgumentNullException.ThrowIfNull(updated);
        var current = Products.FirstOrDefault(p =>
            p.Code.Equals(originalCode?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (current is null)
            throw new InvalidOperationException("الصنف المطلوب تعديله غير موجود.");

        ValidateProduct(updated, current);
        var codeChanged = !current.Code.Equals(updated.Code.Trim(), StringComparison.OrdinalIgnoreCase);
        if (codeChanged && Invoices.SelectMany(i => i.Lines).Any(l =>
            l.ProductCode.Equals(current.Code, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("لا يمكن تغيير كود صنف مرتبط بفواتير سابقة؛ لتجنب كسر سجل المبيعات.");

        current.Code = NormalizeProductCode(updated.Code);
        current.Barcode = updated.Barcode.Trim();
        current.Name = updated.Name.Trim();
        current.Category = updated.Category.Trim();
        current.Price = updated.Price;
        current.Cost = updated.Cost;
        current.MinimumStock = updated.MinimumStock;
        SaveChanges();
    }

    private static string NormalizeProductCode(string? code) =>
        (code ?? string.Empty).Normalize(System.Text.NormalizationForm.FormKC).Trim();

    private void ValidateProduct(Product product, Product? current)
    {
        product.Code = NormalizeProductCode(product.Code);
        if (string.IsNullOrWhiteSpace(product.Code))
            throw new InvalidOperationException("كود الصنف مطلوب.");
        if (string.IsNullOrWhiteSpace(product.Name))
            throw new InvalidOperationException("اسم الصنف مطلوب.");
        if (string.IsNullOrWhiteSpace(product.Barcode))
            throw new InvalidOperationException("الباركود مطلوب.");
        if (product.Price < 0 || product.Cost < 0)
            throw new InvalidOperationException("السعر والتكلفة لا يمكن أن يكونا بالسالب.");
        if (product.MinimumStock < 0)
            throw new InvalidOperationException("حد التنبيه لا يمكن أن يكون بالسالب.");
        if (product.Stock < 0)
            throw new InvalidOperationException("رصيد المخزون لا يمكن أن يكون بالسالب.");

        bool duplicateCode = Products.Any(p => !ReferenceEquals(p, current) &&
            NormalizeProductCode(p.Code).Equals(NormalizeProductCode(product.Code), StringComparison.OrdinalIgnoreCase));
        if (duplicateCode)
            throw new InvalidOperationException("كود الصنف مستخدم بالفعل.");
        bool duplicateBarcode = Products.Any(p => !ReferenceEquals(p, current) &&
            p.Barcode.Equals(product.Barcode.Trim(), StringComparison.OrdinalIgnoreCase));
        if (duplicateBarcode)
            throw new InvalidOperationException("الباركود مستخدم بالفعل لصنف آخر.");
    }

    public int UpdatePrices(IEnumerable<string> productCodes, decimal value, bool percentage)
    {
        if (percentage && (value <= -100 || value > 1000))
            throw new InvalidOperationException("نسبة التغيير يجب أن تكون أكبر من -100% ولا تتجاوز 1000%.");
        var codes = productCodes.Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (codes.Count == 0)
            throw new InvalidOperationException("حدد صنفاً واحداً على الأقل.");
        var selected = Products.Where(p => codes.Contains(p.Code)).ToList();
        if (selected.Count != codes.Count)
            throw new InvalidOperationException("بعض الأصناف المحددة لم تعد موجودة.");
        var proposedPrices = selected.Select(product => new
        {
            Product = product,
            Price = decimal.Round(percentage
                ? product.Price * (1m + value / 100m)
                : product.Price + value, 2, MidpointRounding.AwayFromZero)
        }).ToList();
        if (proposedPrices.Any(item => item.Price < 0))
            throw new InvalidOperationException("التحديث سيؤدي إلى سعر سالب لصنف واحد على الأقل؛ لم يتم تعديل أي سعر.");
        foreach (var item in proposedPrices)
            item.Product.Price = item.Price;
        SaveChanges();
        return selected.Count;
    }

    public string GenerateUniqueBarcode()
    {
        // توليد رقم داخلي فريد من 13 خانة؛ لا يُغيّر الباركود إلا بطلب المستخدم.
        for (int attempt = 0; attempt < 1000; attempt++)
        {
            string candidate = "2000" + Random.Shared.Next(0, 1_000_000_000).ToString("D9");
            if (Products.All(p => !p.Barcode.Equals(candidate, StringComparison.OrdinalIgnoreCase)))
                return candidate;
        }
        throw new InvalidOperationException("تعذر توليد باركود فريد؛ حاول مرة أخرى.");
    }

    public Product? FindProduct(string query)
    {
        string search = query.Trim();
        return Products.FirstOrDefault(p =>
            p.Code.Equals(search, StringComparison.OrdinalIgnoreCase) ||
            p.Barcode.Equals(search, StringComparison.OrdinalIgnoreCase) ||
            p.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
    }
    public Customer? FindCustomer(string query)
    {
        string search = query.Trim();
        return Customers.FirstOrDefault(c =>
            c.Code.Equals(search, StringComparison.OrdinalIgnoreCase) ||
            c.Phone.Contains(search) ||
            c.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
    }
    public Invoice SaveSale(
        List<InvoiceLine> lines,
        string type,
        Customer? customer,
        decimal paid,
        decimal discount,
        decimal discountPercent = 0,
        int installmentMonths = 0,
        IEnumerable<Installment>? schedule = null,
        string representativeCode = "",
        string initialPaymentMethod = "نقدي")
    {
        if (lines.Count == 0)
        {
            throw new InvalidOperationException(
                "أضف صنفاً واحداً على الأقل.");
        }
        foreach (var line in lines)
        {
            var product = Products.First(
                p => p.Code == line.ProductCode);
            if (line.Quantity <= 0)
            {
                throw new InvalidOperationException(
                    "الكمية يجب أن تكون أكبر من صفر.");
            }
            if (line.Quantity > product.Stock)
            {
                throw new InvalidOperationException(
                    $"الكمية المطلوبة من {product.Name} أكبر من المتاح ({product.Stock}).");
            }
        }
        var subtotal = lines.Sum(line => line.Total);
        if (discountPercent < 0 || discountPercent > 100)
            throw new InvalidOperationException("خصم النسبة يجب أن يكون بين 0 و100%.");
        if (discount < 0)
            throw new InvalidOperationException("الخصم لا يمكن أن يكون سالباً.");
        var percentageDiscount = decimal.Round(subtotal * discountPercent / 100m, 2, MidpointRounding.AwayFromZero);
        var total = Math.Max(0, subtotal - percentageDiscount - discount);
        if (discount < 0)
        {
            throw new InvalidOperationException(
                "الخصم لا يمكن أن يكون سالباً.");
        }
        if (paid < 0 || paid > total)
        {
            throw new InvalidOperationException(
                "راجع قيمة المقدم؛ يجب أن تكون بين صفر وإجمالي الفاتورة.");
        }
        if (paid > 0 && string.IsNullOrWhiteSpace(initialPaymentMethod))
            throw new InvalidOperationException("حدد طريقة دفع المقدم.");
        if (type != "كاش" && customer == null)
        {
            throw new InvalidOperationException(
                "اختر العميل للبيع بالتقسيط أو الآجل.");
        }
        if (type == "آجل تاجر" && customer?.Type != "تاجر")
            throw new InvalidOperationException("اختر سجلاً من نوع تاجر للبيع الآجل.");
        if (type == "قسط أفراد" && customer?.Type != "فرد" && customer?.Type != "تاجر")
            throw new InvalidOperationException("اختر عميلاً صالحاً للبيع بالتقسيط.");
        var representative = string.IsNullOrWhiteSpace(representativeCode)
            ? null
            : Representatives.FirstOrDefault(x => x.Code.Equals(representativeCode.Trim(), StringComparison.OrdinalIgnoreCase) && x.IsActive)
                ?? throw new InvalidOperationException("المندوب المحدد غير نشط أو غير موجود.");
        var invoice = new Invoice
        {
            Number = $"INV-{++invoiceSequence}",
            Date = DateTime.Now,
            CustomerName = customer?.Name ?? "عميل نقدي",
            CustomerCode = customer?.Code ?? "",
            RepresentativeCode = representative?.Code ?? "",
            RepresentativeName = representative?.Name ?? "",
            Type = type,
            Total = total,
            DiscountPercent = discountPercent,
            InstallmentMonths = installmentMonths,
            InitialPayment = paid,
            Paid = paid,
            Status = total == paid
                ? "مكتملة"
                : type == "كاش"
                    ? "مدفوع جزئياً"
                    : "رصيد قائم",
            Lines = lines.Select(line => new InvoiceLine
            {
                Id = string.IsNullOrWhiteSpace(line.Id) ? Guid.NewGuid().ToString("N") : line.Id,
                ProductCode = line.ProductCode,
                ProductName = line.ProductName,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                Discount = line.Discount,
                CostAtSale = Products.FirstOrDefault(p => p.Code == line.ProductCode)?.Cost
            }).ToList()
        };
        var scheduleItems = schedule?.OrderBy(x => x.DueDate).ToList() ?? new List<Installment>();
        if (schedule is not null && (type != "قسط أفراد" || scheduleItems.Count == 0 || scheduleItems.Any(x => x.Amount <= 0 || x.DueDate == default) || scheduleItems.Sum(x => x.Amount) != invoice.Remaining))
            throw new InvalidOperationException("جدول الأقساط يجب أن يخص بيع التقسيط وأن يساوي مجموعُه المبلغ المتبقي.");
        var invoiceSequenceBefore = invoiceSequence - 1;
        var receiptSequenceBefore = receiptSequence;
        var customerBalanceBefore = customer?.Balance;
        var productStocksBefore = lines
            .Select(line => Products.First(product => product.Code == line.ProductCode))
            .Distinct()
            .ToDictionary(product => product, product => product.Stock);
        var addedMovements = new List<StockMovement>();
        var addedInstallments = new List<Installment>();
        Payment? initialPayment = null;
        var initialPaymentAdded = false;
        var createdAccruals = new List<CommissionAccrual>();
        try
        {
            foreach (var line in lines)
            {
                var product = Products.First(p => p.Code == line.ProductCode);
                product.Stock -= line.Quantity;
                var movement = new StockMovement
                {
                    Date = DateTime.Now,
                    ProductCode = product.Code,
                    ProductName = product.Name,
                    Type = "بيع",
                    Quantity = -line.Quantity,
                    Reason = invoice.Number
                };
                Movements.Insert(0, movement);
                addedMovements.Add(movement);
            }

            Invoices.Insert(0, invoice);
            for (int i = 0; i < scheduleItems.Count; i++)
            {
                scheduleItems[i].InvoiceNumber = invoice.Number;
                scheduleItems[i].CustomerName = invoice.CustomerName;
                scheduleItems[i].Sequence = i + 1;
                Installments.Add(scheduleItems[i]);
                addedInstallments.Add(scheduleItems[i]);
            }
            if (customer is not null)
                customer.Balance += invoice.Remaining;

            if (paid > 0)
            {
                var sourceId = GetInvoiceInitialPaymentSourceId(invoice.Number);
                initialPayment = Payments.FirstOrDefault(payment =>
                    payment.SourceType == PaymentSourceType.InvoiceInitialPayment &&
                    payment.SourceId.Equals(sourceId, StringComparison.Ordinal));
                if (initialPayment is not null)
                {
                    if (initialPayment.Amount != paid || initialPayment.Allocations.Count != 1 ||
                        !initialPayment.Allocations[0].InvoiceNumber.Equals(invoice.Number, StringComparison.OrdinalIgnoreCase) ||
                        initialPayment.Allocations[0].Amount != paid)
                        throw new InvalidOperationException("مصدر الدفعة المقدمة المحفوظ لا يتطابق مع الفاتورة.");
                }
                else
                {
                    initialPayment = new Payment
                    {
                        ReceiptNumber = $"REC-{++receiptSequence}",
                        Date = invoice.Date,
                        CustomerName = invoice.CustomerName,
                        CustomerCode = invoice.CustomerCode,
                        Amount = paid,
                        Method = initialPaymentMethod.Trim(),
                        BalanceBefore = customerBalanceBefore.GetValueOrDefault() + invoice.Total,
                        BalanceAfter = customerBalanceBefore.GetValueOrDefault() + invoice.Remaining,
                        Note = $"دفعة مقدمة عند إنشاء الفاتورة {invoice.Number}",
                        SourceType = PaymentSourceType.InvoiceInitialPayment,
                        SourceId = sourceId,
                        Allocations =
                        {
                            new PaymentAllocation { InvoiceNumber = invoice.Number, Amount = paid }
                        }
                    };
                    Payments.Insert(0, initialPayment);
                    initialPaymentAdded = true;
                }

                createdAccruals = CreateCommissionAccruals(initialPayment);
                CommissionAccruals.AddRange(createdAccruals);
            }

            SaveChanges();
        }
        catch
        {
            foreach (var accrual in createdAccruals) CommissionAccruals.Remove(accrual);
            if (initialPaymentAdded && initialPayment is not null) Payments.Remove(initialPayment);
            foreach (var installment in addedInstallments) Installments.Remove(installment);
            Invoices.Remove(invoice);
            foreach (var movement in addedMovements) Movements.Remove(movement);
            foreach (var (product, stock) in productStocksBefore) product.Stock = stock;
            if (customer is not null && customerBalanceBefore.HasValue)
                customer.Balance = customerBalanceBefore.Value;
            invoiceSequence = invoiceSequenceBefore;
            receiptSequence = receiptSequenceBefore;
            throw;
        }
        return invoice;
    }

    private static string GetInvoiceInitialPaymentSourceId(string invoiceNumber) =>
        $"invoice-initial-payment:{invoiceNumber}";
    public void AddInstallmentSchedule(Invoice invoice, IEnumerable<Installment> schedule)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(schedule);
        if (invoice.Type != "قسط أفراد")
            throw new InvalidOperationException("جدول الأقساط متاح لفواتير قسط الأفراد فقط.");
        if (Installments.Any(x => x.InvoiceNumber == invoice.Number))
            throw new InvalidOperationException("يوجد جدول أقساط مسجل لهذه الفاتورة بالفعل.");

        var items = schedule.OrderBy(x => x.DueDate).ToList();
        if (invoice.Remaining <= 0)
            throw new InvalidOperationException("لا يوجد مبلغ متبقٍ لإنشاء جدول أقساط.");
        if (items.Count == 0)
            throw new InvalidOperationException("أضف قسطاً واحداً على الأقل.");
        if (items.Any(x => x.Amount <= 0))
            throw new InvalidOperationException("قيمة كل قسط يجب أن تكون أكبر من صفر.");
        if (items.Any(x => x.DueDate == default))
            throw new InvalidOperationException("حدد تاريخ استحقاق لكل قسط.");
        var scheduledTotal = items.Sum(x => x.Amount);
        if (scheduledTotal != invoice.Remaining)
            throw new InvalidOperationException($"مجموع الأقساط ({scheduledTotal:N2}) يجب أن يساوي المتبقي بالفاتورة ({invoice.Remaining:N2}) جنيه.");

        for (int i = 0; i < items.Count; i++)
        {
            items[i].InvoiceNumber = invoice.Number;
            items[i].CustomerName = invoice.CustomerName;
            items[i].Sequence = i + 1;
            items[i].PaidAmount = 0;
            Installments.Add(items[i]);
        }
        SaveChanges();
    }

    public IReadOnlyList<Installment> GetInstallments(string invoiceNumber) => Installments
        .Where(x => x.InvoiceNumber.Equals(invoiceNumber, StringComparison.OrdinalIgnoreCase))
        .OrderBy(x => x.Sequence)
        .ToList();

    /// <summary>Returns sales linked to a party by stable code, with an unambiguous name fallback for legacy invoices.</summary>
    public IReadOnlyList<Invoice> GetCustomerInvoices(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        var legacyNameIsUnique = Customers.Count(x => x.Name.Equals(customer.Name, StringComparison.OrdinalIgnoreCase)) == 1;
        return Invoices.Where(invoice =>
                !string.IsNullOrWhiteSpace(invoice.CustomerCode)
                    ? invoice.CustomerCode.Equals(customer.Code, StringComparison.OrdinalIgnoreCase)
                    : legacyNameIsUnique && invoice.CustomerName.Equals(customer.Name, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Date).ThenBy(x => x.Number, StringComparer.Ordinal).ToList();
    }

    /// <summary>Returns each payment once if it belongs to the party or is allocated to one of its invoices.</summary>
    public IReadOnlyList<Payment> GetCustomerPayments(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        var invoiceNumbers = GetCustomerInvoices(customer).Select(x => x.Number).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var legacyNameIsUnique = Customers.Count(x => x.Name.Equals(customer.Name, StringComparison.OrdinalIgnoreCase)) == 1;
        return Payments.Where(payment => !string.IsNullOrWhiteSpace(payment.CustomerCode)
                ? payment.CustomerCode.Equals(customer.Code, StringComparison.OrdinalIgnoreCase)
                : payment.Allocations.Any(allocation => invoiceNumbers.Contains(allocation.InvoiceNumber)) ||
                  (legacyNameIsUnique && payment.CustomerName.Equals(customer.Name, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(x => x.Date).ThenBy(x => x.ReceiptNumber, StringComparer.Ordinal).ToList();
    }

    /// <summary>Collections for one invoice; payment-to-installment details are included only when explicitly stored.</summary>
    public IReadOnlyList<Payment> GetPaymentsForInvoice(string invoiceNumber) => Payments
        .Where(payment => payment.Allocations.Any(allocation => allocation.InvoiceNumber.Equals(invoiceNumber, StringComparison.OrdinalIgnoreCase)) ||
                          (payment.SourceType == PaymentSourceType.InvoiceInitialPayment &&
                           payment.SourceId.Equals(GetInvoiceInitialPaymentSourceId(invoiceNumber), StringComparison.Ordinal)) ||
                          Installments.Any(installment => installment.InvoiceNumber.Equals(invoiceNumber, StringComparison.OrdinalIgnoreCase) &&
                              installment.Id.Equals(payment.InstallmentId, StringComparison.Ordinal)))
        .OrderBy(x => x.Date).ThenBy(x => x.ReceiptNumber, StringComparer.Ordinal).ToList();

    public Payment CollectForInstallment(Installment installment, decimal amount, string method, string note)
    {
        ArgumentNullException.ThrowIfNull(installment);
        amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (!Installments.Contains(installment))
            throw new InvalidOperationException("القسط المحدد غير موجود في جدول الأقساط الحالي.");
        if (amount <= 0)
            throw new InvalidOperationException("أدخل مبلغ تحصيل أكبر من صفر.");
        if (amount > installment.Remaining)
            throw new InvalidOperationException($"مبلغ التحصيل أكبر من المتبقي على القسط ({installment.Remaining:N2} ج.م).");

        var invoice = Invoices.FirstOrDefault(x => x.Number.Equals(installment.InvoiceNumber, StringComparison.OrdinalIgnoreCase));
        if (invoice is null)
            throw new InvalidOperationException("لم يتم العثور على الفاتورة المرتبطة بالقسط.");
        var customer = !string.IsNullOrWhiteSpace(invoice.CustomerCode)
            ? Customers.FirstOrDefault(x => x.Code.Equals(invoice.CustomerCode, StringComparison.OrdinalIgnoreCase))
            : Customers.Where(x => x.Name.Equals(installment.CustomerName, StringComparison.OrdinalIgnoreCase)).Take(2).ToList() is { Count: 1 } legacyMatches
                ? legacyMatches[0]
                : null;
        if (customer is null)
            throw new InvalidOperationException("لم يتم العثور على العميل المرتبط بالقسط.");
        if (amount > customer.Balance)
            throw new InvalidOperationException($"مبلغ التحصيل أكبر من رصيد العميل ({customer.Balance:N2} ج.م).");
        if (amount > invoice.Remaining)
            throw new InvalidOperationException($"مبلغ التحصيل أكبر من المتبقي على الفاتورة ({invoice.Remaining:N2} ج.م).");

        var before = customer.Balance;
        var installmentPaidBefore = installment.PaidAmount;
        var invoicePaidBefore = invoice.Paid;
        var invoiceStatusBefore = invoice.Status;
        installment.PaidAmount = decimal.Round(installment.PaidAmount + amount, 2, MidpointRounding.AwayFromZero);
        customer.Balance = decimal.Round(customer.Balance - amount, 2, MidpointRounding.AwayFromZero);
        invoice.Paid = decimal.Round(invoice.Paid + amount, 2, MidpointRounding.AwayFromZero);
        invoice.Status = invoice.Remaining <= 0 ? "مكتملة" : "رصيد قائم";

        var payment = new Payment
        {
            ReceiptNumber = $"REC-{++receiptSequence}",
            Date = DateTime.Now,
            CustomerName = customer.Name,
            CustomerCode = customer.Code,
            InstallmentId = installment.Id,
            Amount = amount,
            Method = method,
            BalanceBefore = before,
            BalanceAfter = customer.Balance,
            Note = $"فاتورة {invoice.Number} | القسط {installment.Sequence} | {note?.Trim()}"
        };
        payment.Allocations.Add(new PaymentAllocation { InvoiceNumber = invoice.Number, Amount = amount });
        var createdAccruals = CreateCommissionAccruals(payment);
        CommissionAccruals.AddRange(createdAccruals);
        Payments.Insert(0, payment);
        try
        {
            SaveChanges();
        }
        catch
        {
            Payments.Remove(payment);
            foreach (var accrual in createdAccruals) CommissionAccruals.Remove(accrual);
            installment.PaidAmount = installmentPaidBefore;
            customer.Balance = before;
            invoice.Paid = invoicePaidBefore;
            invoice.Status = invoiceStatusBefore;
            throw;
        }
        return payment;
    }

    public Payment Collect(
        Customer customer,
        decimal amount,
        string method,
        string note)
    {
        amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (amount <= 0)
        {
            throw new InvalidOperationException(
                "أدخل مبلغاً أكبر من صفر.");
        }
        if (amount > customer.Balance)
        {
            throw new InvalidOperationException(
                "مبلغ التحصيل أكبر من رصيد العميل.");
        }
        var allocations = new List<PaymentAllocation>();
        var outstandingInvoices = Invoices
            .Where(invoice =>
                (!string.IsNullOrWhiteSpace(invoice.CustomerCode) &&
                 invoice.CustomerCode.Equals(customer.Code, StringComparison.OrdinalIgnoreCase)) ||
                (string.IsNullOrWhiteSpace(invoice.CustomerCode) &&
                 invoice.CustomerName.Equals(customer.Name, StringComparison.OrdinalIgnoreCase)))
            .Where(invoice => invoice.Remaining > 0)
            .OrderBy(invoice => invoice.Date)
            .ThenBy(invoice => invoice.Number, StringComparer.Ordinal)
            .ToList();
        var unapplied = amount;
        foreach (var invoice in outstandingInvoices)
        {
            if (unapplied <= 0) break;
            var applied = Math.Min(unapplied, invoice.Remaining);
            if (applied <= 0) continue;
            allocations.Add(new PaymentAllocation { InvoiceNumber = invoice.Number, Amount = applied });
            unapplied = decimal.Round(unapplied - applied, 2, MidpointRounding.AwayFromZero);
        }

        var invoiceBefore = allocations.Select(allocation =>
        {
            var invoice = Invoices.First(x => x.Number.Equals(allocation.InvoiceNumber, StringComparison.OrdinalIgnoreCase));
            return (Invoice: invoice, Paid: invoice.Paid, Status: invoice.Status, Amount: allocation.Amount);
        }).ToList();
        var installmentBefore = new Dictionary<Installment, decimal>();
        var before = customer.Balance;
        customer.Balance = decimal.Round(customer.Balance - amount, 2, MidpointRounding.AwayFromZero);
        foreach (var item in invoiceBefore)
        {
            item.Invoice.Paid += item.Amount;
            item.Invoice.Status = item.Invoice.Remaining <= 0
                ? "\u0645\u0643\u062A\u0645\u0644\u0629"
                : "\u0631\u0635\u064A\u062F \u0642\u0627\u0626\u0645";
            var installmentAmount = item.Amount;
            foreach (var installment in Installments
                .Where(x => x.InvoiceNumber.Equals(item.Invoice.Number, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Sequence))
            {
                if (installmentAmount <= 0) break;
                var installmentApplied = Math.Min(installmentAmount, installment.Remaining);
                if (installmentApplied <= 0) continue;
                installmentBefore.TryAdd(installment, installment.PaidAmount);
                installment.PaidAmount += installmentApplied;
                installmentAmount = decimal.Round(installmentAmount - installmentApplied, 2, MidpointRounding.AwayFromZero);
            }
        }
        var payment = new Payment
        {
            ReceiptNumber = $"REC-{++receiptSequence}",
            Date = DateTime.Now,
            CustomerName = customer.Name,
            CustomerCode = customer.Code,
            Amount = amount,
            Method = method,
            BalanceBefore = before,
            BalanceAfter = customer.Balance,
            Note = note,
            Allocations = allocations
        };
        var createdAccruals = CreateCommissionAccruals(payment);
        CommissionAccruals.AddRange(createdAccruals);
        Payments.Insert(0, payment);
        try
        {
            SaveChanges();
        }
        catch
        {
            Payments.Remove(payment);
            foreach (var accrual in createdAccruals) CommissionAccruals.Remove(accrual);
            customer.Balance = before;
            foreach (var item in invoiceBefore)
            {
                item.Invoice.Paid = item.Paid;
                item.Invoice.Status = item.Status;
            }
            foreach (var item in installmentBefore)
                item.Key.PaidAmount = item.Value;
            throw;
        }
        return payment;
    }
    public void AdjustStock(
        Product product,
        decimal actual,
        string reason)
    {
        if (actual < 0)
        {
            throw new InvalidOperationException(
                "الكمية الفعلية لا يمكن أن تكون سالبة.");
        }
        var difference = actual - product.Stock;
        if (difference == 0)
        {
            throw new InvalidOperationException(
                "لا يوجد فرق لتسويته.");
        }
        product.Stock = actual;
        Movements.Insert(0, new StockMovement
        {
            Date = DateTime.Now,
            ProductCode = product.Code,
            ProductName = product.Name,
            Type = "تسوية جرد",
            Quantity = difference,
            Reason = reason
        });
        SaveChanges();
    }
    public Expense AddExpense(
        string category,
        string description,
        decimal amount,
        string paymentMethod,
        string createdBy,
        string notes)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new InvalidOperationException("اختر تصنيف المصروف.");
        if (string.IsNullOrWhiteSpace(description))
            throw new InvalidOperationException("اكتب وصف المصروف.");
        if (amount <= 0)
            throw new InvalidOperationException(
                "مبلغ المصروف يجب أن يكون أكبر من صفر.");
        if (string.IsNullOrWhiteSpace(paymentMethod))
            throw new InvalidOperationException("اختر طريقة الدفع.");
        var expense = new Expense
        {
            Number = $"EXP-{++expenseSequence:0000}",
            Date = DateTime.Now,
            Category = category.Trim(),
            Description = description.Trim(),
            Amount = amount,
            PaymentMethod = paymentMethod.Trim(),
            CreatedBy = string.IsNullOrWhiteSpace(createdBy)
                ? "موظف تجريبي"
                : createdBy.Trim(),
            Notes = notes?.Trim() ?? ""
        };
        Expenses.Insert(0, expense);
        SaveChanges();
        return expense;
    }
}
