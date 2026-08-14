using System.Windows.Input;

namespace VideoFetch.App.ViewModels;

public sealed class AsyncRelayCommand(
    Func<CancellationToken, Task> execute,
    Func<bool>? canExecute = null,
    Action<Exception>? onException = null) : ICommand
{
    private CancellationTokenSource? cancellationSource;
    private bool isRunning;

    public event EventHandler? CanExecuteChanged;

    public bool IsRunning => isRunning;

    public bool CanExecute(object? parameter) => !isRunning && (canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        isRunning = true;
        cancellationSource = new CancellationTokenSource();
        NotifyCanExecuteChanged();
        try
        {
            await execute(cancellationSource.Token);
        }
        catch (OperationCanceledException)
        {
            // Cancellation is reflected by the owning view model.
        }
        catch (Exception exception)
        {
            onException?.Invoke(exception);
        }
        finally
        {
            cancellationSource.Dispose();
            cancellationSource = null;
            isRunning = false;
            NotifyCanExecuteChanged();
        }
    }

    public void Cancel() => cancellationSource?.Cancel();

    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
