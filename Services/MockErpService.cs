
using SmartErpDesktop.Models;

namespace SmartErpDesktop.Services;

/// <summary>
/// In-memory demo service.
/// Replaced by repositories after UI approval.
/// </summary>
public sealed class MockErpService
{
    public List<Product> Products { get; } = new();
    public List<Customer> Customers { get; } = new();
    public List<Invoice> Invoices { get; } = new();
    public List<Payment> Payments { get; } = new();
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

        // بيانات فواتير تجريبية بتواريخ سابقة.
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

        return invoice;
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

        return expense;
    }

}
