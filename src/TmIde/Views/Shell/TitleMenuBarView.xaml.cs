using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TmIde.Views.Shell;

public partial class TitleMenuBarView : UserControl
{
    private Window? _hostWindow;

    public TitleMenuBarView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _hostWindow = Window.GetWindow(this);
        if (_hostWindow is not null)
        {
            _hostWindow.StateChanged += OnHostStateChanged;
            UpdateMaximizeGlyph();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_hostWindow is not null)
        {
            _hostWindow.StateChanged -= OnHostStateChanged;
        }
    }

    private void OnHostStateChanged(object? sender, EventArgs e) => UpdateMaximizeGlyph();

    private void OnMinimize(object sender, RoutedEventArgs e)
    {
        if (_hostWindow is not null)
        {
            _hostWindow.WindowState = WindowState.Minimized;
        }
    }

    private void OnMaximizeRestore(object sender, RoutedEventArgs e)
    {
        if (_hostWindow is null)
        {
            return;
        }

        _hostWindow.WindowState = _hostWindow.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void OnClose(object sender, RoutedEventArgs e) => _hostWindow?.Close();

    private void UpdateMaximizeGlyph()
    {
        if (_hostWindow is null)
        {
            return;
        }

        var key = _hostWindow.WindowState == WindowState.Maximized ? "IconCaptionRestore" : "IconCaptionMax";
        if (TryFindResource(key) is Geometry geometry)
        {
            MaximizeGlyph.Data = geometry;
        }
    }
}
