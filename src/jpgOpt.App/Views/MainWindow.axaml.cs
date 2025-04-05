using Avalonia.Controls;
using jpgOpt.App.ViewModels;

namespace jpgOpt.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DataContext = new MainWindowViewModel();
    }
}