using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;

namespace TmIde.Services;

public sealed class NamePromptService : INamePromptService
{
    // Fuente de la interfaz (Styles.xaml); sin Application se usa Consolas.
    private static FontFamily UiFontFamily() =>
        Application.Current?.TryFindResource("UiFontFamily") as FontFamily ?? new FontFamily("Consolas");

    // Tamaño de la escala tipográfica común (Styles.xaml), o el respaldo si no existe.
    private static double FontSizeOrDefault(string key, double fallback) =>
        Application.Current?.TryFindResource(key) is double size ? size : fallback;

    public string? RequestName(
        string title,
        string message,
        string initialName = "")
    {
        var owner = Application.Current?.MainWindow;

        var dialog = new Window
        {
            Title = title,
            Width = 460,
            Height = 230,
            MinWidth = 420,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = owner is null
                ? WindowStartupLocation.CenterScreen
                : WindowStartupLocation.CenterOwner,
            Owner = owner,
            ShowInTaskbar = false
        };

        dialog.SetResourceReference(
            Window.BackgroundProperty,
            "SurfaceBackgroundBrush");

        // Estructura principal
        var layout = new Grid
        {
            Margin = new Thickness(24, 22, 24, 20)
        };

        layout.RowDefinitions.Add(new RowDefinition
        {
            Height = GridLength.Auto
        });

        layout.RowDefinitions.Add(new RowDefinition
        {
            Height = GridLength.Auto
        });

        layout.RowDefinitions.Add(new RowDefinition
        {
            Height = new GridLength(1, GridUnitType.Star)
        });

        layout.RowDefinitions.Add(new RowDefinition
        {
            Height = GridLength.Auto
        });

        // Etiqueta
        var label = new TextBlock
        {
            Text = message,
            FontSize = FontSizeOrDefault("FontSizeTitle", 15),
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 14),
            TextWrapping = TextWrapping.Wrap
        };

        label.SetResourceReference(
            TextBlock.ForegroundProperty,
            "PrimaryForegroundBrush");

        Grid.SetRow(label, 0);
        layout.Children.Add(label);

        var inputBorder = new Border
        {
            Height = 46,
            CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 0, 0, 12)
        };

        inputBorder.SetResourceReference(
            Border.BackgroundProperty,
            "HoverBackgroundBrush");

        inputBorder.SetResourceReference(
            Border.BorderBrushProperty,
            "SecondaryForegroundBrush");

        // Campo de texto con estilo independiente 
        var input = new TextBox
        {
            Style = new Style(typeof(TextBox)),
            Text = initialName,
            Height = 44,
            FontSize = FontSizeOrDefault("FontSizeTitle", 15),
            FontFamily = UiFontFamily(),
            Padding = new Thickness(12, 0, 12, 0),
            Margin = new Thickness(0),
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Left
        };

        input.SetResourceReference(
            TextBox.ForegroundProperty,
            "PrimaryForegroundBrush");

        input.SetResourceReference(
            TextBox.CaretBrushProperty,
            "PrimaryForegroundBrush");

        input.SetResourceReference(
            TextBox.SelectionBrushProperty,
            "SurfaceAccentBrush");

        inputBorder.Child = input;

        Grid.SetRow(inputBorder, 1);
        layout.Children.Add(inputBorder);

        // Contenedor de botones
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0)
        };

        // Botón Cancelar
        var cancelButton = new Button
        {
            Content = "Cancelar",
            Width = 110,
            Height = 38,
            FontSize = FontSizeOrDefault("FontSizeEmphasis", 13),
            FontWeight = FontWeights.SemiBold,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 0, 10, 0),
            IsCancel = true
        };

        cancelButton.SetResourceReference(
            Button.BackgroundProperty,
            "HoverBackgroundBrush");

        cancelButton.SetResourceReference(
            Button.ForegroundProperty,
            "PrimaryForegroundBrush");

        cancelButton.SetResourceReference(
            Button.BorderBrushProperty,
            "SecondaryForegroundBrush");

        // Botón Aceptar
        var acceptButton = new Button
        {
            Content = "Aceptar",
            Width = 110,
            Height = 38,
            FontSize = FontSizeOrDefault("FontSizeEmphasis", 13),
            FontWeight = FontWeights.SemiBold,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            IsDefault = true
        };

        acceptButton.SetResourceReference(
            Button.ForegroundProperty,
            "SurfaceAccentForegroundBrush");

        acceptButton.SetResourceReference(
            Button.BackgroundProperty,
            "SurfaceAccentBrush");

        acceptButton.SetResourceReference(
            Button.BorderBrushProperty,
            "SurfaceAccentBrush");

        string? result = null;

        cancelButton.Click += (_, _) =>
        {
            dialog.DialogResult = false;
        };

        acceptButton.Click += (_, _) =>
        {
            result = input.Text;
            dialog.DialogResult = true;
        };

        buttons.Children.Add(cancelButton);
        buttons.Children.Add(acceptButton);

        Grid.SetRow(buttons, 3);
        layout.Children.Add(buttons);

        dialog.Content = layout;

        // Enfocar el campo al abrir
        dialog.Loaded += (_, _) =>
        {
            input.Focus();
            input.CaretIndex = input.Text.Length;
        };

        return dialog.ShowDialog() == true
            ? result
            : null;
    }
}