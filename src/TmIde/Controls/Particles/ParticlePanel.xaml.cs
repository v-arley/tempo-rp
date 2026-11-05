using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using TmIde.ViewModels.Particles;

namespace TmIde.Controls.Particles;

/// <summary>
/// Bucle de animación del panel de partículas: en cada fotograma copia del
/// <see cref="ParticlePanelViewModel"/> el ánimo, el tempo y las ráfagas
/// nuevas, avanza la <see cref="ParticleSimulation"/>, la dibuja con el
/// <see cref="DotRenderer"/> y vuelca el lienzo en un mapa de bits. Solo
/// anima mientras está en pantalla.
/// </summary>

public partial class ParticlePanel : UserControl
{
    // Unos 30 fotogramas por segundo.
    private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(33);

    // Un tirón (ventana arrastrada, depurador) no debe hacer saltar la animación.
    private const double MaxFrameSeconds = 0.1;

    private readonly ParticleSimulation _simulation = new();
    private readonly DispatcherTimer _frames = new(DispatcherPriority.Render) { Interval = FrameInterval };
    private readonly Stopwatch _frameClock = new();

    private DotCanvas _canvas = new(0, 0);
    private WriteableBitmap? _bitmap;

    public ParticlePanel()
    {
        InitializeComponent();

        _frames.Tick += (_, _) => DrawNextFrame();
        Loaded += (_, _) => StartAnimation();
        Unloaded += (_, _) => _frames.Stop();
        Stage.SizeChanged += (_, _) => FitCanvasToStage();
    }

    private void StartAnimation()
    {
        FitCanvasToStage();
        _frameClock.Restart();
        _frames.Start();
    }

    private void DrawNextFrame()
    {
        if (DataContext is not ParticlePanelViewModel panel || _bitmap is null)
        {
            return;
        }

        var seconds = Math.Min(_frameClock.Elapsed.TotalSeconds, MaxFrameSeconds);
        _frameClock.Restart();

        _simulation.Mood = panel.Mood;
        _simulation.TempoBpm = panel.TempoBpm;

        foreach (var intensity in panel.TakeBursts())
        {
            _simulation.Pulse(intensity);
        }

        _simulation.Advance(seconds);
        _canvas.Background = StageBackground();
        DotRenderer.Render(_simulation.Points(), _canvas, _simulation.Palette);

        _bitmap.WritePixels(new Int32Rect(0, 0, _canvas.Width, _canvas.Height), _canvas.Pixels, _canvas.Stride, 0);
    }

    // El fondo del escenario es un recurso del tema: al cambiar de tema cambia
    // el pincel, y el siguiente fotograma ya se pinta sobre el color nuevo.
    private DotColor StageBackground() => Stage.Background is SolidColorBrush { Color: var color }
        ? new DotColor(color.R, color.G, color.B)
        : DotCanvas.DefaultBackground;

    // Un píxel del lienzo por píxel físico de la pantalla: los puntos se ven
    // nítidos también con el escalado de Windows al 125 % o 150 %.
    private void FitCanvasToStage()
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        var width = (int)(Stage.ActualWidth * dpi.DpiScaleX);
        var height = (int)(Stage.ActualHeight * dpi.DpiScaleY);

        if (width <= 0 || height <= 0 || (width == _canvas.Width && height == _canvas.Height))
        {
            return;
        }

        _canvas = new DotCanvas(width, height);
        _bitmap = new WriteableBitmap(width, height, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Bgr32, null);
        DotLayer.Source = _bitmap;
    }
}
