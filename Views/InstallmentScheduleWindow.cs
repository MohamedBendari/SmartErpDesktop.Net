using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SmartErpDesktop.Models;

namespace SmartErpDesktop.Views;

/// <summary>إدخال قيم ومواعيد الأقساط يدويًا قبل اعتماد فاتورة التقسيط.</summary>
public sealed class InstallmentScheduleWindow : Window
{
    private readonly decimal expectedAmount;
    private readonly List<Installment> items = new();
    private readonly DatePicker dueDatePicker = new();
    private readonly TextBox amountBox = new();
    private readonly DataGrid scheduleGrid = new();
    private readonly TextBlock totalText = new();

    public List<Installment> ResultInstallments { get; private set; } = new();

    public InstallmentScheduleWindow(decimal expectedRemaining)
    {
        expectedAmount = decimal.Round(expectedRemaining, 2, MidpointRounding.AwayFromZero);
        Title = "جدول أقساط يدوي";
        Width = 760;
        Height = 540;
        MinWidth = 650;
        MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FlowDirection = FlowDirection.RightToLeft;
        Background = System.Windows.Media.Brushes.White;

        var root = new Grid { Margin = new Thickness(16) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var heading = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        heading.Children.Add(new TextBlock { Text = "تحديد مواعيد وقيم الأقساط", FontSize = 22, FontWeight = FontWeights.Bold });
        heading.Children.Add(new TextBlock { Text = $"المبلغ المطلوب توزيعه: {expectedAmount:N2} ج.م — أدخل قيمة وتاريخ كل قسط، ويجب أن يساوي المجموع هذا المبلغ.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 0) });
        Grid.SetRow(heading, 0);
        root.Children.Add(heading);

        var entry = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        entry.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
        entry.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        entry.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        dueDatePicker.SelectedDate = DateTime.Today.AddMonths(1);
        dueDatePicker.Margin = new Thickness(3);
        amountBox.Text = "0";
        amountBox.Margin = new Thickness(3);
        amountBox.MinWidth = 130;
        var addButton = new Button { Content = "إضافة قسط", Padding = new Thickness(14, 7, 14, 7), Margin = new Thickness(3) };
        addButton.Click += Add_Click;
        entry.Children.Add(dueDatePicker);
        Grid.SetColumn(amountBox, 1); entry.Children.Add(amountBox);
        Grid.SetColumn(addButton, 2); entry.Children.Add(addButton);
        Grid.SetRow(entry, 1); root.Children.Add(entry);

        scheduleGrid.AutoGenerateColumns = true;
        scheduleGrid.IsReadOnly = true;
        scheduleGrid.CanUserAddRows = false;
        scheduleGrid.Margin = new Thickness(0, 0, 0, 8);
        scheduleGrid.ItemsSource = items;
        Grid.SetRow(scheduleGrid, 2); root.Children.Add(scheduleGrid);

        var footer = new DockPanel { LastChildFill = false };
        totalText.FontSize = 15;
        totalText.FontWeight = FontWeights.SemiBold;
        totalText.VerticalAlignment = VerticalAlignment.Center;
        UpdateTotal();
        DockPanel.SetDock(totalText, Dock.Right); footer.Children.Add(totalText);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
        var removeButton = new Button { Content = "حذف المحدد", Background = System.Windows.Media.Brushes.IndianRed, Padding = new Thickness(12, 7, 12, 7) };
        removeButton.Click += (_, _) =>
        {
            if (scheduleGrid.SelectedItem is Installment selected) { items.Remove(selected); RefreshGrid(); }
        };
        var saveButton = new Button { Content = "اعتماد جدول الأقساط", Padding = new Thickness(14, 7, 14, 7) };
        saveButton.Click += Save_Click;
        var cancelButton = new Button { Content = "إلغاء", Background = System.Windows.Media.Brushes.SlateGray, Padding = new Thickness(14, 7, 14, 7) };
        cancelButton.Click += (_, _) => DialogResult = false;
        buttons.Children.Add(removeButton); buttons.Children.Add(saveButton); buttons.Children.Add(cancelButton);
        DockPanel.SetDock(buttons, Dock.Left); footer.Children.Add(buttons);
        Grid.SetRow(footer, 3); root.Children.Add(footer);
        Content = root;
    }

    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        if (dueDatePicker.SelectedDate is not DateTime dueDate)
        {
            MessageBox.Show("حدد تاريخ استحقاق القسط.");
            return;
        }
        if (!decimal.TryParse(amountBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount) &&
            !decimal.TryParse(amountBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
        {
            MessageBox.Show("أدخل قيمة رقمية صحيحة للقسط.");
            return;
        }
        amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (amount <= 0)
        {
            MessageBox.Show("قيمة القسط يجب أن تكون أكبر من صفر.");
            return;
        }
        if (items.Sum(x => x.Amount) + amount > expectedAmount)
        {
            MessageBox.Show("إجمالي الأقساط بعد الإضافة سيتجاوز المبلغ المتبقي بالفاتورة.");
            return;
        }
        items.Add(new Installment { Sequence = items.Count + 1, DueDate = dueDate.Date, Amount = amount });
        amountBox.Text = "0";
        dueDatePicker.SelectedDate = dueDate.Date.AddMonths(1);
        RefreshGrid();
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        if (items.Count == 0)
        {
            MessageBox.Show("أضف قسطاً واحداً على الأقل.");
            return;
        }
        var sum = decimal.Round(items.Sum(x => x.Amount), 2, MidpointRounding.AwayFromZero);
        if (sum != expectedAmount)
        {
            MessageBox.Show($"مجموع الأقساط {sum:N2} ج.م، والمطلوب {expectedAmount:N2} ج.م. عدّل القيم قبل الاعتماد.");
            return;
        }
        ResultInstallments = items.OrderBy(x => x.DueDate).Select((x, index) => new Installment
        {
            Sequence = index + 1,
            DueDate = x.DueDate,
            Amount = x.Amount
        }).ToList();
        DialogResult = true;
    }

    private void RefreshGrid()
    {
        scheduleGrid.ItemsSource = null;
        scheduleGrid.ItemsSource = items;
        UpdateTotal();
    }

    private void UpdateTotal()
    {
        var sum = items.Sum(x => x.Amount);
        totalText.Text = $"المجموع: {sum:N2} / {expectedAmount:N2} ج.م";
    }
}
