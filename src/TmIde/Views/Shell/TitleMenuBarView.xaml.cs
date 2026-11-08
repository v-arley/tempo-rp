using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TmIde.ViewModels;

namespace TmIde.Views.Shell;

public partial class TitleMenuBarView : UserControl
{
    /// Indica si la barra muestra los menús principales.
    /// True para TMIDE y false para ventanas auxiliares.
    public static readonly DependencyProperty ShowMenusProperty =
        DependencyProperty.Register(
            nameof(ShowMenus),
            typeof(bool),
            typeof(TitleMenuBarView),
            new PropertyMetadata(true));

    public bool ShowMenus
    {
        get => (bool)GetValue(ShowMenusProperty);
        set => SetValue(ShowMenusProperty, value);
    }
    
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

    
    /// Aumenta la octava musical desde el menú del piano.
    private void OnPianoOctaveUp(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel
            { Piano: { } piano })
        {
            piano.ChangeOctave(1);
        }
    }

    /// Reduce la octava musical desde el menú del piano.
    private void OnPianoOctaveDown(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel
            { Piano: { } piano })
        {
            piano.ChangeOctave(-1);
        }
    }


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
