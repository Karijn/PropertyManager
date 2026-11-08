using System.Windows;
using PropertyManager.Services;
using PropertyManager.ViewModels;

namespace PropertyManager;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }

}
