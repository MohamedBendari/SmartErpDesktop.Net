# Requirements checklist — Smart ERP Desktop Client

Source: attached combined project presentation, requirements and development roadmap (52 pages). Status refers to this prototype, not production readiness.

## Foundation / shell
- [x] WPF .NET 8 Windows project and Visual Studio solution
- [x] Arabic RTL shell, navigation and shared styles
- [x] Global search (product/customer/invoice)
- [x] Dashboard with limited operational status and recent transactions
- [x] Mock data/services; no database or API
- [ ] Formal screen-by-screen UI approval and user usability testing
- [ ] Dedicated automated unit/integration test suite

## Sales / invoices
- [x] Product search by name, code or barcode
- [x] Cart add/remove and editable quantity/line discount
- [x] Subtotal, invoice discount, total and remaining amount
- [x] Sale types cash/installment/trader credit
- [x] Customer required for installment/credit
- [x] Stock availability validation and stock movement on sale
- [x] Demo invoice number, timestamp, status and in-memory save
- [x] Duplicate submit guard for current sale view
- [x] Invoice list, search, details and text preview
- [ ] Installment schedule generation, due dates and per-installment balance allocation
- [ ] Return and cancellation flow, reason, approval and original-document linkage
- [ ] Real thermal/A4 printing, printer selection, PDF output and barcode label printing

## Customers / collections
- [x] Customer/trader list, contact data and current balance
- [x] Collection form, payment method, note and positive/outstanding-balance validation
- [x] Before/after balance in receipt record
- [ ] Full chronological account statement, related invoice/payment timeline
- [ ] Installment allocation, overdue/future due indicators and receipt reprint screen
- [ ] Customer registration/editing and validated trader credit limits

## Products / stock / purchasing
- [x] Product code, barcode, category, sale price, cost, stock and minimum stock
- [x] Low-stock indicators
- [x] Stock adjustment with reason and movement record
- [x] Purchase/supplier sample records
- [ ] Product create/edit and barcode/QR generation
- [ ] Purchase invoice entry with line items and stock receiving
- [ ] Purchase returns linked to original purchase and supplier balance
- [ ] Formal stock-count session and discrepancy approval

## Other operations / quality
- [ ] Expenses workflow (fields and approval rules need definition)
- [ ] Settings and print preferences
- [ ] UI-level role/permission behavior and permission review
- [ ] Comprehensive audit log for sensitive operations
- [ ] Backup/restore workflows (deferred until persistence design)
- [x] Basic empty/search validation messages and preview dialogs
- [ ] Full Loading/Empty/Error/Disabled/No Permission states across every screen
- [ ] Keyboard accessibility, mixed Arabic/Latin testing and RTL review on Windows
- [ ] Windows build, runtime smoke test, regression tests and release handover

## Decision log
1. In-memory data resets on application exit, as required by UI-first scope.
2. Discount entered as a fixed amount; percentage discounts and role limits require approval.
3. Installment calculations are not fabricated; frequency, due-date rules, rounding and allocation policy require approval.
4. Returns/cancellations require a defined policy for stock and account adjustments.
5. Actual printer type, paper size, receipt template, PDF export and barcode symbology require confirmation.
6. The requirements mention multi-device/local database and backup in a later phase; no storage decision is implemented before UI approval.
