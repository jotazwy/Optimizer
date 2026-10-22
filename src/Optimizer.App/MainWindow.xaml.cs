using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Runtime.InteropServices;
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
        var workArea = SystemParameters.WorkArea;
        Width = Math.Max(MinWidth, Math.Min(Width, workArea.Width - 32));
        Height = Math.Max(MinHeight, Math.Min(Height, workArea.Height - 32));
        Left = workArea.Left + (workArea.Width - Width) / 2;
        Top = workArea.Top + (workArea.Height - Height) / 2;
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

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        var enabled = 1;
        var round = 2;
        // Older Windows versions return an unsupported-attribute HRESULT; the
        // in-app glass surfaces work independently of the optional DWM chrome.
        _ = DwmSetWindowAttribute(handle, 20, ref enabled, sizeof(int));
        _ = DwmSetWindowAttribute(handle, 33, ref round, sizeof(int));
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized) SystemCommands.RestoreWindow(this);
        else SystemCommands.MaximizeWindow(this);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => SystemCommands.CloseWindow(this);

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        if (MaximizeGlyph is null) return;
        var maximized = WindowState == WindowState.Maximized;
        MaximizeGlyph.Data = Geometry.Parse(maximized
            ? "M2.5,0.5 H9.5 V7.5 M0.5,2.5 H7.5 V9.5 H0.5 Z"
            : "M0.5,0.5 H9.5 V9.5 H0.5 Z");
        MaximizeButton.ToolTip = maximized ? "Restaurar" : "Maximizar";
        System.Windows.Automation.AutomationProperties.SetName(MaximizeButton, maximized ? "Restaurar" : "Maximizar");
        WindowFrame.Margin = maximized ? new Thickness(7) : new Thickness(0);
    }

    private void Navigate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        FrameworkElement target = (button.CommandParameter as string) switch
        {
            "Resources" => ResourcesSection,
            "Hardware" => HardwareSection,
            "Processes" => ProcessesSection,
            "Insights" => InsightsSection,
            _ => OverviewSection
        };
        var position = target.TranslatePoint(new Point(0, 0), DashboardContent);
        MainScroll.ScrollToVerticalOffset(Math.Max(0, position.Y - 12));
        foreach (var navigation in new[] { OverviewNav, ResourcesNav, HardwareNav, ProcessesNav, InsightsNav })
        {
            navigation.Tag = ReferenceEquals(navigation, button) ? "Active" : null;
        }
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(nint handle, int attribute, ref int value, int size);

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
