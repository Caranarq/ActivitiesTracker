using System.Windows;
using ActivitiesTracker.UI.ViewModels;

namespace ActivitiesTracker.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
