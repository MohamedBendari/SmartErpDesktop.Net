using SmartErpDesktop.Models;
namespace SmartErpDesktop.Services;
public sealed class MockErpService
{
    public List<Product> Products { get; } = new();
    public List<Customer> Customers { get; } = new();
    public List<Invoice> Invoices { get; } = new();
    public List<Payment> Payments { get; } = new();
    public List<Installment> Installments { get; } = new();
    public List<Supplier> Suppliers { get; } = new();
    public List<Purchase> Purchases { get; } = new();
    public List<StockMovement> Movements { get; } = new();
    public List<Expense> Expenses { get; } = new();
    private int expenseSequence = 0;
    private int invoiceSequence = 1004;
    private int receiptSequence = 501;
    public MockErpService()
    {
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
        if (ErpDataStore.TryLoad(this, out var savedState))
        {
            Products.Clear(); Products.AddRange(savedState.Products);
            Customers.Clear(); Customers.AddRange(savedState.Customers);
            Invoices.Clear(); Invoices.AddRange(savedState.Invoices);
            Payments.Clear(); Payments.AddRange(savedState.Payments);
            Installments.Clear(); Installments.AddRange(savedState.Installments);
            Suppliers.Clear(); Suppliers.AddRange(savedState.Suppliers);
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

    /// <summary>Persist all operational collections atomically to the shared SQLite database.</summary>
    public void SaveChanges() => ErpDataStore.Save(this);
    public IReadOnlyList<Product> LowStockProducts => Products
        .Where(p => p.Stock <= p.MinimumStock)
        .OrderBy(p => p.Stock)
        .ToList();

    public void AddProduct(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        ValidateProduct(product, null);
        product.Code = product.Code.Trim();
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

        current.Code = updated.Code.Trim();
        current.Barcode = updated.Barcode.Trim();
        current.Name = updated.Name.Trim();
        current.Category = updated.Category.Trim();
        current.Price = updated.Price;
        current.Cost = updated.Cost;
        current.MinimumStock = updated.MinimumStock;
        SaveChanges();
    }

    private void ValidateProduct(Product product, Product? current)
    {
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
            p.Code.Equals(product.Code.Trim(), StringComparison.OrdinalIgnoreCase));
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
        decimal discount)
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
        var total = Math.Max(0, subtotal - discount);
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
        if (type != "كاش" && customer == null)
        {
            throw new InvalidOperationException(
                "اختر العميل للبيع بالتقسيط أو الآجل.");
        }
        var invoice = new Invoice
        {
            Number = $"INV-{++invoiceSequence}",
            Date = DateTime.Now,
            CustomerName = customer?.Name ?? "عميل نقدي",
            Type = type,
            Total = total,
            Paid = paid,
            Status = total == paid
                ? "مكتملة"
                : type == "كاش"
                    ? "مدفوع جزئياً"
                    : "رصيد قائم",
            Lines = lines.Select(line => new InvoiceLine
            {
                ProductCode = line.ProductCode,
                ProductName = line.ProductName,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                Discount = line.Discount
            }).ToList()
        };
        foreach (var line in lines)
        {
            var product = Products.First(
                p => p.Code == line.ProductCode);
            product.Stock -= line.Quantity;
            Movements.Insert(0, new StockMovement
            {
                Date = DateTime.Now,
                ProductCode = product.Code,
                ProductName = product.Name,
                Type = "بيع",
                Quantity = -line.Quantity,
                Reason = invoice.Number
            });
        }
        Invoices.Insert(0, invoice);
        if (customer != null)
        {
            customer.Balance += invoice.Remaining;
        }
        SaveChanges();
        return invoice;
    }
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

    public Payment CollectForInstallment(Installment installment, decimal amount, string method, string note)
    {
        ArgumentNullException.ThrowIfNull(installment);
        if (!Installments.Contains(installment))
            throw new InvalidOperationException("القسط المحدد غير موجود في جدول الأقساط الحالي.");
        if (amount <= 0)
            throw new InvalidOperationException("أدخل مبلغ تحصيل أكبر من صفر.");
        if (amount > installment.Remaining)
            throw new InvalidOperationException($"مبلغ التحصيل أكبر من المتبقي على القسط ({installment.Remaining:N2} ج.م).");

        var invoice = Invoices.FirstOrDefault(x => x.Number.Equals(installment.InvoiceNumber, StringComparison.OrdinalIgnoreCase));
        if (invoice is null)
            throw new InvalidOperationException("لم يتم العثور على الفاتورة المرتبطة بالقسط.");
        var customer = Customers.FirstOrDefault(x => x.Name.Equals(installment.CustomerName, StringComparison.OrdinalIgnoreCase));
        if (customer is null)
            throw new InvalidOperationException("لم يتم العثور على العميل المرتبط بالقسط.");
        if (amount > customer.Balance)
            throw new InvalidOperationException($"مبلغ التحصيل أكبر من رصيد العميل ({customer.Balance:N2} ج.م).");
        if (amount > invoice.Remaining)
            throw new InvalidOperationException($"مبلغ التحصيل أكبر من المتبقي على الفاتورة ({invoice.Remaining:N2} ج.م).");

        var before = customer.Balance;
        installment.PaidAmount = decimal.Round(installment.PaidAmount + amount, 2, MidpointRounding.AwayFromZero);
        customer.Balance = decimal.Round(customer.Balance - amount, 2, MidpointRounding.AwayFromZero);
        invoice.Paid = decimal.Round(invoice.Paid + amount, 2, MidpointRounding.AwayFromZero);
        invoice.Status = invoice.Remaining <= 0 ? "مكتملة" : "رصيد قائم";

        var payment = new Payment
        {
            ReceiptNumber = $"REC-{++receiptSequence}",
            Date = DateTime.Now,
            CustomerName = customer.Name,
            Amount = amount,
            Method = method,
            BalanceBefore = before,
            BalanceAfter = customer.Balance,
            Note = $"فاتورة {invoice.Number} | القسط {installment.Sequence} | {note?.Trim()}"
        };
        Payments.Insert(0, payment);
        SaveChanges();
        return payment;
    }

    public Payment Collect(
        Customer customer,
        decimal amount,
        string method,
        string note)
    {
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
        var before = customer.Balance;
        customer.Balance -= amount;
        var payment = new Payment
        {
            ReceiptNumber = $"REC-{++receiptSequence}",
            Date = DateTime.Now,
            CustomerName = customer.Name,
            Amount = amount,
            Method = method,
            BalanceBefore = before,
            BalanceAfter = customer.Balance,
            Note = note
        };
        Payments.Insert(0, payment);
        SaveChanges();
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
