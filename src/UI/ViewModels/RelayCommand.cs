using System.Windows.Input;

namespace ActivitiesTracker.UI.ViewModels;

public sealed class RelayCommand(Action execute) : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => execute();
}
