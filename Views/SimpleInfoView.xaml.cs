using System.Windows.Controls;namespace SmartErpDesktop.Views;
public partial class SimpleInfoView:UserControl
{public SimpleInfoView(string title,string message){InitializeComponent();
        Heading.Text=title;Message.Text=message;}}