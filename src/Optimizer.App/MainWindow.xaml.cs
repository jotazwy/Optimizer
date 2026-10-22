using System.Windows;
using System.Windows.Threading;
using Optimizer.App.Services;

namespace Optimizer.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private readonly WindowsDiagnosticService _diagnostics = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(2) };
    private bool _refreshing;
    private bool _closed;
    private bool _hasSample;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        _timer.Tick += Timer_Tick;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _viewModel.SetBusy(true);
        try
        {
            var hardware = await Task.Run(_diagnostics.GetHardwareInfo);
            if (_closed) return;
            _viewModel.SetHardware(hardware);
        }
        catch (Exception exception)
        {
            if (!_closed) _viewModel.SetError(exception.Message);
        }

        if (_closed) return;
        await RefreshAsync();
        if (!_closed) _timer.Start();
    }

    private async void Timer_Tick(object? sender, EventArgs e) => await RefreshAsync();
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        if (_closed || _refreshing) return;
        _refreshing = true;
        _viewModel.SetBusy(true);
        try
        {
            var snapshot = await Task.Run(_diagnostics.CaptureSnapshot);
            if (_closed) return;
            _viewModel.Apply(snapshot, warmingUp: !_hasSample);
            _hasSample = true;
        }
        catch (Exception exception)
        {
            if (!_closed) _viewModel.SetError(exception.Message);
        }
        finally
        {
            _refreshing = false;
            if (!_closed) _viewModel.SetBusy(false);
        }
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        _closed = true;
        _timer.Stop();
        _timer.Tick -= Timer_Tick;
    }
}
