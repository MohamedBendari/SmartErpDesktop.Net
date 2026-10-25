using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
 using global::SmartErpDesktop.Models;
 using global::SmartErpDesktop.Services;
 using System.Globalization;

    namespace SmartErpDesktop.Views;

    public partial class ExpensesView : UserControl
    {
        private readonly MockErpService service;

        public ExpensesView(MockErpService service)
        {
            InitializeComponent();

            this.service = service;

            RefreshExpenses();
        }

        private void SaveExpense_Click(object sender, RoutedEventArgs e)
        {
            string category = CategoryBox.Text.Trim();
            string description = DescriptionBox.Text.Trim();
            string paymentMethod =
                (PaymentMethodBox.SelectedItem as ComboBoxItem)
                ?.Content?.ToString() ?? "";

            string createdBy = CreatedByBox.Text.Trim();
            string notes = NotesBox.Text.Trim();

            if (!decimal.TryParse(
                    AmountBox.Text,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out decimal amount)
                &&
                !decimal.TryParse(
                    AmountBox.Text,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out amount))
            {
                MessageBox.Show(
                    "أدخل مبلغًا رقميًا صحيحًا.",
                    "بيانات المصروف",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            try
            {
                Expense expense = service.AddExpense(
                    category,
                    description,
                    amount,
                    paymentMethod,
                    createdBy,
                    notes);

                RefreshExpenses();

                SearchBox.Clear();

                AmountBox.Clear();
                DescriptionBox.Clear();
                NotesBox.Clear();

                CategoryBox.Text = "أخرى";
                PaymentMethodBox.SelectedIndex = 0;

                MessageBox.Show(
                    $"تم تسجيل المصروف بنجاح.\n" +
                    $"رقم المصروف: {expense.Number}\n" +
                    $"المبلغ: {expense.Amount:N2} ج.م\n" +
                    $"التاريخ: {expense.Date:yyyy/MM/dd hh:mm tt}",
                    "تم الحفظ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    ArabicUi.ErrorMessage(ex, "تعذر تسجيل المصروف. تحقق من البيانات وحاول مرة أخرى."),
                    "تعذر تسجيل المصروف",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void RefreshExpenses()
        {
            ExpensesGrid.ItemsSource = null;
            ExpensesGrid.ItemsSource = service.Expenses.ToList();

            DateTime now = DateTime.Now;

            decimal todayTotal = service.Expenses
                .Where(x => x.Date.Date == now.Date)
                .Sum(x => x.Amount);

            decimal monthTotal = service.Expenses
                .Where(x =>
                    x.Date.Year == now.Year &&
                    x.Date.Month == now.Month)
                .Sum(x => x.Amount);

            TodayTotalText.Text = $"{todayTotal:N2} ج.م";
            MonthTotalText.Text = $"{monthTotal:N2} ج.م";
            ExpenseCountText.Text =
                service.Expenses.Count.ToString("N0");
        }

        private void SearchBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (ExpensesGrid == null)
                return;

            string query = SearchBox.Text.Trim();

            var results = service.Expenses.Where(x =>
                x.Number.Contains(
                    query, StringComparison.OrdinalIgnoreCase) ||
                x.Category.Contains(
                    query, StringComparison.OrdinalIgnoreCase) ||
                x.Description.Contains(
                    query, StringComparison.OrdinalIgnoreCase) ||
                x.PaymentMethod.Contains(
                    query, StringComparison.OrdinalIgnoreCase));

            ExpensesGrid.ItemsSource = results.ToList();
        }

        private void ClearSearch_Click(
            object sender,
            RoutedEventArgs e)
        {
            SearchBox.Clear();
            RefreshExpenses();
        }
    }


