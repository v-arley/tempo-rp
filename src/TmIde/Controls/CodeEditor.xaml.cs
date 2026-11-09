using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Linq;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;
using Tempo.Language.Outline;
using TmIde.Controls.Completion;
using TmIde.Controls.Diagnostics;
using TmIde.Controls.Search;
using TmIde.Controls.Syntax;
using TmIde.Services.Analysis;
using TmIde.Services.Completion;
using TmIde.ViewModels.Editor;
using TmIde.Services.Audio;

namespace TmIde.Controls;

/// <summary>
/// Editor de código TM sobre AvalonEdit con coloreado de sintaxis y subrayado de
/// diagnósticos. Edita el <see cref="Document"/> que se le enlace (uno por
/// pestaña) y recuerda cursor, desplazamiento y plegados de cada documento al
/// cambiar de uno a otro. Incluye búsqueda (Ctrl+F), autocompletado
/// (Ctrl+Espacio), plegado de bloques y sangría automática. Expone <see cref="CaretLine"/> y <see cref="CaretColumn"/> como
/// propiedades de dependencia enlazables.
/// </summary>

public partial class CodeEditor : UserControl
{
    public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(
        nameof(Document),
        typeof(TextDocument),
        typeof(CodeEditor),
        new FrameworkPropertyMetadata(null, OnDocumentChanged));

    public static readonly DependencyProperty CaretLineProperty = DependencyProperty.Register(
        nameof(CaretLine),
        typeof(int),
        typeof(CodeEditor),
        new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty CaretColumnProperty = DependencyProperty.Register(
        nameof(CaretColumn),
        typeof(int),
        typeof(CodeEditor),
        new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty NavigationRequestProperty = DependencyProperty.Register(
        nameof(NavigationRequest),
        typeof(EditorNavigationRequest),
        typeof(CodeEditor),
        new PropertyMetadata(null, OnNavigationRequestChanged));    

    public static readonly DependencyProperty DiagnosticsProperty = DependencyProperty.Register(
        nameof(Diagnostics),
        typeof(IReadOnlyList<EditorDiagnostic>),
        typeof(CodeEditor),
        new FrameworkPropertyMetadata(null, OnDiagnosticsChanged));

    public static readonly DependencyProperty PianoProperty = DependencyProperty.Register(
        nameof(Piano),
        typeof(TypingPianoService),
        typeof(CodeEditor),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ActionRequestProperty = DependencyProperty.Register(
        nameof(ActionRequest),
        typeof(EditorActionRequest),
        typeof(CodeEditor),
        new PropertyMetadata(null, OnActionRequestChanged));

    public static readonly DependencyProperty OutlineProperty = DependencyProperty.Register(
        nameof(Outline),
        typeof(FileOutline),
        typeof(CodeEditor),
        new FrameworkPropertyMetadata(FileOutline.Empty));

    public static readonly DependencyProperty SelectionAccentBrushProperty = RegisterThemeBrush(nameof(SelectionAccentBrush));

    public static readonly DependencyProperty CaretBrushProperty = RegisterThemeBrush(nameof(CaretBrush));

    public static readonly DependencyProperty FoldingMarkerBrushProperty = RegisterThemeBrush(nameof(FoldingMarkerBrush));

    public static readonly DependencyProperty SelectedFoldingMarkerBrushProperty = RegisterThemeBrush(nameof(SelectedFoldingMarkerBrush));

    public static readonly DependencyProperty FoldingMarkerBackgroundBrushProperty = RegisterThemeBrush(nameof(FoldingMarkerBackgroundBrush));

    private const double SearchMarkerOpacity = 0.4;

    private readonly TmSyntaxColorizer _syntaxColorizer = new();
    private readonly DiagnosticsRenderer _diagnosticsRenderer;
    private readonly ToolTip _diagnosticToolTip = new() { Placement = PlacementMode.Mouse, StaysOpen = true };

    // Tooltip informativo de palabras reservadas.
    private readonly ToolTip _keywordToolTip = new()
    {
        Placement = PlacementMode.Mouse,
        StaysOpen = true
    };

    // Cursor y desplazamiento de cada documento mientras no es el visible. La
    // tabla es débil: al cerrar una pestaña y soltarse su documento, el estado se va con él.
    private readonly ConditionalWeakTable<TextDocument, ViewState> _viewStates = new();

    private sealed record ViewState(
        int CaretOffset,
        double VerticalOffset,
        double HorizontalOffset,
        IReadOnlyCollection<int> FoldedStarts);

    // Los plegados se recalculan tras una pausa al escribir, no en cada pulsación.
    private readonly DispatcherTimer _foldingTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private readonly SearchPanel _searchPanel;
    private readonly TmIndentationStrategy _indentationStrategy = new();
    private readonly EditorCompletionController _completion;
    private readonly FindReplaceController _findReplace;
    private FoldingManager? _foldingManager;

    public CodeEditor()
    {
        InitializeComponent();

        var textView = Editor.TextArea.TextView;
        textView.LineTransformers.Add(_syntaxColorizer);
        _diagnosticsRenderer = new DiagnosticsRenderer(textView);
        textView.BackgroundRenderers.Add(_diagnosticsRenderer);
        textView.MouseHover += OnTextViewMouseHover;
        textView.MouseHoverStopped += OnTextViewMouseHoverStopped;
        _diagnosticToolTip.ContentTemplate = (DataTemplate)Resources["DiagnosticToolTipTemplate"];
        _diagnosticToolTip.SetResourceReference(BackgroundProperty, "SurfaceBackgroundBrush");
        _diagnosticToolTip.SetResourceReference(ForegroundProperty, "PrimaryForegroundBrush");
        _diagnosticToolTip.SetResourceReference(Control.BorderBrushProperty, "DividerBrush");

        // Aplicar el diseño de ayuda a las palabras reservadas.
        _keywordToolTip.ContentTemplate = (DataTemplate)Resources["KeywordToolTipTemplate"];
        _keywordToolTip.SetResourceReference(Control.BackgroundProperty, "SurfaceBackgroundBrush");
        _keywordToolTip.SetResourceReference(Control.ForegroundProperty, "PrimaryForegroundBrush");
        _keywordToolTip.SetResourceReference(Control.BorderBrushProperty, "DividerBrush");

        // Detectar Ctrl cuando el mouse ya está sobre una palabra.
        Editor.PreviewKeyDown += OnEditorPreviewKeyDown;

        // Ocultar la ayuda al soltar Ctrl.
        Editor.PreviewKeyUp += OnEditorPreviewKeyUp;

        Editor.TextChanged += OnEditorTextChanged;
        _searchPanel = SearchPanel.Install(Editor);
        _findReplace = new FindReplaceController(_searchPanel, Editor.TextArea, () => Editor.IsReadOnly);

        // Sangría de 4 espacios (Tab inserta espacios, no tabulaciones); Enter la
        // hereda y '<' al inicio de línea cierra el bloque.
        Editor.Options.IndentationSize = TmIndentationStrategy.IndentSize;
        Editor.Options.ConvertTabsToSpaces = true;
        Editor.TextArea.IndentationStrategy = _indentationStrategy;
        Editor.TextArea.TextEntered += OnTextEntered;
        // Modo interpretación: usar las teclas como piano antes de que AvalonEdit procese el texto.
        Editor.TextArea.PreviewKeyDown += OnPianoPerformanceKeyDown;
        // Detectar cuando se suelta una tecla musical.
        Editor.TextArea.PreviewKeyUp += OnPianoPerformanceKeyUp;
        // Liberar las notas cuando el editor pierde el foco.
        Editor.TextArea.LostKeyboardFocus += OnPianoEditorLostKeyboardFocus;
        // Enter y Backspace no siempre generan TextEntered, por lo que se detectan por separado.
        Editor.TextArea.PreviewKeyDown += OnPianoSpecialKeyDown;

        // Autocompletado: se abre al escribir o con Ctrl+Espacio. Los nombres del
        // usuario salen del resumen enlazado en Outline, que se lee en cada petición.
        _completion = new EditorCompletionController(
            Editor.TextArea,
            CompletionEngine.WithSymbols(() => Outline ?? FileOutline.Empty));

        _foldingTimer.Tick += OnFoldingTimerTick;
        InstallFolding();
        Editor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;

        SetResourceReference(SelectionAccentBrushProperty, "SurfaceAccentBrush");
        SetResourceReference(CaretBrushProperty, "PrimaryForegroundBrush");
        SetResourceReference(FoldingMarkerBrushProperty, "TertiaryForegroundBrush");
        SetResourceReference(SelectedFoldingMarkerBrushProperty, "SecondaryForegroundBrush");
        SetResourceReference(FoldingMarkerBackgroundBrushProperty, "SurfaceBackgroundBrush");
    }

    /// <summary>Documento que se edita; si es nulo el editor queda vacío y de solo lectura.</summary>
    public TextDocument? Document
    {
        get => (TextDocument?)GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    public EditorNavigationRequest? NavigationRequest
    {
        get => (EditorNavigationRequest?)
            GetValue(NavigationRequestProperty);

        set => SetValue(NavigationRequestProperty, value);
    }

    public int CaretLine
    {
        get => (int)GetValue(CaretLineProperty);
        set => SetValue(CaretLineProperty, value);
    }

    public int CaretColumn
    {
        get => (int)GetValue(CaretColumnProperty);
        set => SetValue(CaretColumnProperty, value);
    }

    /// <summary>Diagnósticos del documento actual; el control los subraya y muestra su detalle al pasar el mouse.</summary>
    public IReadOnlyList<EditorDiagnostic>? Diagnostics
    {
        get => (IReadOnlyList<EditorDiagnostic>?)GetValue(DiagnosticsProperty);
        set => SetValue(DiagnosticsProperty, value);
    }

    /// <summary>Acción pedida desde fuera del editor (menú EDITAR); se ejecuta con <see cref="Execute"/>.</summary>
    public EditorActionRequest? ActionRequest
    {
        get => (EditorActionRequest?)GetValue(ActionRequestProperty);
        set => SetValue(ActionRequestProperty, value);
    }

    // Se ejecuta cuando el menú ya se cerró, para que el foco quede en el editor o en el panel.
    private static void OnActionRequestChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is EditorActionRequest request)
        {
            var editor = (CodeEditor)d;
            editor.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => editor.Execute(request.Action)));
        }
    }

    /// <summary>Funciones y nombres del documento actual; alimentan el autocompletado.</summary>
    public FileOutline? Outline
    {
        get => (FileOutline?)GetValue(OutlineProperty);
        set => SetValue(OutlineProperty, value);
    }

    /// <summary>Color base de la selección y de las coincidencias de búsqueda (se aplica con transparencia).</summary>
    public Brush? SelectionAccentBrush
    {
        get => (Brush?)GetValue(SelectionAccentBrushProperty);
        set => SetValue(SelectionAccentBrushProperty, value);
    }

    public Brush? CaretBrush
    {
        get => (Brush?)GetValue(CaretBrushProperty);
        set => SetValue(CaretBrushProperty, value);
    }

    /// <summary>Marcadores del margen de plegado y texto de un bloque plegado.</summary>
    public Brush? FoldingMarkerBrush
    {
        get => (Brush?)GetValue(FoldingMarkerBrushProperty);
        set => SetValue(FoldingMarkerBrushProperty, value);
    }

    public Brush? SelectedFoldingMarkerBrush
    {
        get => (Brush?)GetValue(SelectedFoldingMarkerBrushProperty);
        set => SetValue(SelectedFoldingMarkerBrushProperty, value);
    }

    public Brush? FoldingMarkerBackgroundBrush
    {
        get => (Brush?)GetValue(FoldingMarkerBackgroundBrushProperty);
        set => SetValue(FoldingMarkerBackgroundBrushProperty, value);
    }

    /// Servicio de notas musicales para este editor.
    public TypingPianoService? Piano
    {
        get => (TypingPianoService?)GetValue(PianoProperty);
        set => SetValue(PianoProperty, value);
    }

    private static DependencyProperty RegisterThemeBrush(string name) => DependencyProperty.Register(
        name,
        typeof(Brush),
        typeof(CodeEditor),
        new FrameworkPropertyMetadata(null, OnThemeBrushChanged));

    private static void OnDiagnosticsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((CodeEditor)d).ApplyDiagnostics();

    private void ApplyDiagnostics()
    {
        _diagnosticToolTip.IsOpen = false;
        if (Diagnostics is { Count: > 0 } diagnostics)
        {
            _diagnosticsRenderer.Update(diagnostics, Editor.Document);
        }
        else
        {
            _diagnosticsRenderer.Clear();
        }
    }

    
    /// Si el mouse ya está sobre una palabra y el usuario presiona Ctrl, intenta mostrar la información.
    private void OnEditorPreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && EditorShortcuts.TryGetValue(e.Key, out var action))
        {
            Execute(action);
            e.Handled = true;
            return;
        }

        if (!KeywordHoverOptions.RequireControlKey)
        {
            return;
        }

        if (e.Key is not (Key.LeftCtrl or Key.RightCtrl))
        {
            return;
        }

        // Se ejecuta después de procesar la tecla, para que Keyboard.Modifiers esté actualizado.
        Dispatcher.BeginInvoke(
            DispatcherPriority.Input,
            new Action(() =>
            {
                var textView = Editor.TextArea.TextView;

                if (textView.IsMouseOver)
                {
                    ShowHoverInformation(
                        Mouse.GetPosition(textView));
                }
            }));
    }

    // Atajos de edición con Ctrl: tecla → acción. El menú EDITAR dispara las
    // mismas acciones por EditorActionRequest.
    private static readonly Dictionary<Key, EditorAction> EditorShortcuts = new()
    {
        [Key.Space] = EditorAction.Complete,
        [Key.F] = EditorAction.Find,
        [Key.H] = EditorAction.Replace,
    };

    /// <summary>Ejecuta una acción de edición sobre el documento visible.</summary>
    public void Execute(EditorAction action)
    {
        switch (action)
        {
            case EditorAction.Find:
                _findReplace.OpenFind();
                break;
            case EditorAction.Replace:
                _findReplace.OpenReplace();
                break;
            case EditorAction.Complete:
                Editor.TextArea.Focus();
                _completion.ShowSuggestions();
                break;
        }
    }

    /// Al soltar Ctrl se oculta la ayuda educativa.
    /// El tooltip de errores permanece independiente.
    private void OnEditorPreviewKeyUp(
        object sender,
        KeyEventArgs e)
    {
        if (!KeywordHoverOptions.RequireControlKey)
        {
            return;
        }

        if (e.Key is Key.LeftCtrl or Key.RightCtrl)
        {
            _keywordToolTip.IsOpen = false;
        }
    }


    // El aspecto del tooltip está en DiagnosticToolTipTemplate (XAML); aquí solo
    // se eligen los diagnósticos que hay bajo el puntero.
    
    /// Se ejecuta cuando el mouse permanece sobre el código.
    /// Los diagnósticos tienen prioridad sobre la ayuda.
    private void OnTextViewMouseHover(object? sender, MouseEventArgs e)
    {
        var textView = Editor.TextArea.TextView;

        ShowHoverInformation(
            e.GetPosition(textView));
    }

    
    /// Muestra información contextual del código.
    /// Prioridad:
    /// 1. Palabras reservadas de Tempo.
    /// 2. Errores y advertencias del compilador.
    /// La opción RequireControlKey permite elegir
    /// entre Ctrl + Hover y hover automático.
    private void ShowHoverInformation(Point mousePosition)
    {
        // Cerrar cualquier tooltip anterior
        _diagnosticToolTip.IsOpen = false;
        _keywordToolTip.IsOpen = false;

        var textView = Editor.TextArea.TextView;

        if (!textView.IsMouseOver)
        {
            return;
        }

        // Obtener la posición del mouse sobre el código
        var position = textView.GetPositionFloor(
            mousePosition + textView.ScrollOffset);

        if (position is null)
        {
            return;
        }

        var document = Editor.Document;

        var offset = document.GetOffset(
            position.Value.Location);

        // PRIORIDAD 1: PALABRAS RESERVADAS
        // ==================================================

        // Consultar si hay una palabra reservada
        var keyword = _syntaxColorizer.GetReservedWordAt(
            document,
            offset);

        if (keyword is not null)
        {
            // Consultar su equivalencia y descripción
            var info = TempoKeywordReference.Find(keyword);

            if (info is not null)
            {
                // Verificar si la configuración exige Ctrl
                var canShowKeyword =
                    !KeywordHoverOptions.RequireControlKey ||
                    (Keyboard.Modifiers & ModifierKeys.Control) != 0;

                if (canShowKeyword)
                {
                    _keywordToolTip.Content = info;
                    _keywordToolTip.IsOpen = true;

                    // La palabra reservada tiene prioridad,
                    // aunque tenga un diagnóstico asociado.
                    return;
                }
            }
        }

        // PRIORIDAD 2: ERRORES Y ADVERTENCIAS
        // ==================================================

        // Buscar diagnósticos en la posición del mouse
        var diagnosticsUnderPointer =
            _diagnosticsRenderer.SegmentsAt(offset)
                .Select(segment => segment.Diagnostic)
                .ToList();

        if (diagnosticsUnderPointer.Count > 0)
        {
            _diagnosticToolTip.Content = diagnosticsUnderPointer;
            _diagnosticToolTip.IsOpen = true;
        }
    }


    /// Oculta ambas ventanas cuando termina el hover.
    private void OnTextViewMouseHoverStopped(object? sender, MouseEventArgs e)
    {
        _diagnosticToolTip.IsOpen = false;
        _keywordToolTip.IsOpen = false;
    }

    /// <summary>
    /// Atiende solicitudes externas para mover el cursor.
    /// </summary>
    private static void OnNavigationRequestChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not EditorNavigationRequest request)
        {
            return;
        }

        var control = (CodeEditor)d;

        control.Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(() =>
            {
                // La solicitud pertenece al documento activo
                if (!ReferenceEquals(
                        control.Document,
                        request.Document))
                {
                    return;
                }

                var offset = Math.Clamp(
                    request.Offset,
                    0,
                    control.Editor.Document.TextLength);

                control.Editor.TextArea.Caret.Offset = offset;

                // Mostrar la linea donde queda el cursor
                control.Editor.ScrollToLine(
                    control.Editor.TextArea.Caret.Line);

                // Devolver el foco al editor
                control.Editor.TextArea.Focus();
            }));
    }

    private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((CodeEditor)d).SwitchDocument((TextDocument?)e.OldValue, (TextDocument?)e.NewValue);

    private void SwitchDocument(TextDocument? previous, TextDocument? next)
    {
        // Al cambiar de archivo, liberar las teclas musicales
        Piano?.ReleaseAllPerformanceNotes();

        // El estado se lee antes del cambio: en ese momento el editor aún muestra el anterior.
        if (previous is not null)
        {
            var foldedStarts = _foldingManager?.AllFoldings
                .Where(folding => folding.IsFolded)
                .Select(folding => folding.StartOffset)
                .ToList() ?? [];
            _viewStates.AddOrUpdate(previous, new ViewState(
                Editor.TextArea.Caret.Offset, Editor.VerticalOffset, Editor.HorizontalOffset, foldedStarts));
        }

        _diagnosticToolTip.IsOpen = false;
        _keywordToolTip.IsOpen = false;
        _completion.Close();
        Editor.Document = next ?? new TextDocument();
        Editor.IsReadOnly = next is null;
        _syntaxColorizer.Rescan(Editor.Document);

        // El gestor de plegado está ligado a un documento: se recrea con el nuevo.
        InstallFolding();

        // El orden en que se actualizan los enlaces Document y Diagnostics no está
        // garantizado: se reaplican los diagnósticos contra el documento ya cambiado.
        ApplyDiagnostics();

        if (next is not null && _viewStates.TryGetValue(next, out var state))
        {
            Editor.TextArea.Caret.Offset = Math.Min(state.CaretOffset, next.TextLength);

            if (_foldingManager is not null)
            {
                var previouslyFolded = _foldingManager.AllFoldings
                    .Where(folding => state.FoldedStarts.Contains(folding.StartOffset));
                foreach (var folding in previouslyFolded)
                {
                    folding.IsFolded = true;
                }
            }

            // El desplazamiento solo se puede fijar cuando el nuevo documento ya tiene diseño.
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                Editor.ScrollToVerticalOffset(state.VerticalOffset);
                Editor.ScrollToHorizontalOffset(state.HorizontalOffset);
            }));
        }
    }


    
    /// <summary>
    /// Comprueba que estamos escribiendo código y
    /// no ejecutando un atajo de teclado.
    /// </summary>
    private bool CanPlayPiano()
    {
        if (Piano?.Enabled != true ||
            Piano.IsPerformanceMode ||
            Document is null ||
            Editor.IsReadOnly)
        {
            return false;
        }

        // Evitar sonidos con Ctrl, Alt o Windows.
        // Shift se permite para las mayúsculas.
        const ModifierKeys blocked =
            ModifierKeys.Control |
            ModifierKeys.Alt |
            ModifierKeys.Windows;

        return (Keyboard.Modifiers & blocked) == ModifierKeys.None;
    }

    
    /// En modo interpretación las teclas musicales producen notas sin modificar el documento.
    private void OnPianoPerformanceKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (Piano is not
            { Enabled: true, IsPerformanceMode: true } piano)
        {
            return;
        }

        // Mantener disponibles los atajos del IDE.
        // Shift se permite.
        const ModifierKeys blockedModifiers =
            ModifierKeys.Control |
            ModifierKeys.Alt |
            ModifierKeys.Windows;

        if ((Keyboard.Modifiers & blockedModifiers) !=
            ModifierKeys.None)
        {
            return;
        }

        // Escape vuelve a escritura normal.
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            piano.IsPerformanceMode = false;
            return;
        }

        // Bajar una octava.
        if (e.Key is Key.Z or Key.PageDown)
        {
            e.Handled = true;

            if (!e.IsRepeat)
            {
                piano.ChangeOctave(-1);
            }

            return;
        }

        // Subir una octava.
        if (e.Key is Key.X or Key.PageUp)
        {
            e.Handled = true;

            if (!e.IsRepeat)
            {
                piano.ChangeOctave(1);
            }

            return;
        }

        // Detectar si la tecla representa una nota musical.
        if (PianoPerformanceKeyMap.TryGetMidi(
            e.Key,
            piano.Octave,
            out var midiNote))
        {
            // No escribir el carácter en el editor.
            e.Handled = true;

            if (!e.IsRepeat)
            {
                // La nota seguirá sonando hasta recibir el evento KeyUp.
                piano.StartPerformanceNote(
                    e.Key,
                    midiNote);
            }

            return;
        }

        // Permitir navegación por el código y teclas
        // de función (por ejemplo F1 para AYUDA).
        if (e.Key is
            Key.Left or Key.Right or
            Key.Up or Key.Down or
            Key.Home or Key.End or
            Key.LeftShift or Key.RightShift or
            Key.CapsLock)
        {
            return;
        }

        if (e.Key >= Key.F1 && e.Key <= Key.F12)
        {
            return;
        }

        // Evitar que las demás teclas introduzcan texto,
        // borren caracteres o inserten saltos de línea.
        e.Handled = true;
    }

    
    /// Cuando se libera una tecla, la nota asociada comienza a desvanecerse.
    private void OnPianoPerformanceKeyUp(
        object sender,
        KeyEventArgs e)
    {
        Piano?.StopPerformanceNote(e.Key);
    }

    /// Evita que una nota quede sonando si el usuario cambia de ventana o sale del editor.
    private void OnPianoEditorLostKeyboardFocus(
        object sender,
        KeyboardFocusChangedEventArgs e)
    {
        Piano?.ReleaseAllPerformanceNotes();
    }

    /// Sonidos especiales para Enter y Backspace.
    /// No modifica el comportamiento de esas teclas.
    private void OnPianoSpecialKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (!CanPlayPiano() || e.IsRepeat)
        {
            return;
        }

        if (e.Key == Key.Return || e.Key == Key.Enter)
        {
            Piano?.PlayEnter();
        }
        else if (e.Key == Key.Back)
        {
            Piano?.PlayBackspace();
        }
    }


    // Al escribir '<' como primer carácter de la línea se realinea con el '>' que abrió el bloque.
    private void OnTextEntered(object? sender, TextCompositionEventArgs e)
    {
        // Reproducir la nota de la tecla escrita.
        if (CanPlayPiano() && e.Text.Length == 1)
        {
            Piano?.PlayCharacter(e.Text[0]);
        }

        if (e.Text != "<")
        {
            return;
        }

        var line = Editor.Document.GetLineByOffset(Editor.TextArea.Caret.Offset);
        if (Editor.Document.GetText(line).Trim() == "<")
        {
            _indentationStrategy.IndentLine(Editor.Document, line);
        }
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        _syntaxColorizer.Rescan(Editor.Document);
        _keywordToolTip.IsOpen = false;
        _foldingTimer.Stop();
        _foldingTimer.Start();
    }

    private void InstallFolding()
    {
        _foldingTimer.Stop();
        if (_foldingManager is not null)
        {
            FoldingManager.Uninstall(_foldingManager);
        }

        _foldingManager = FoldingManager.Install(Editor.TextArea);
        TmFoldingStrategy.UpdateFoldings(_foldingManager, Editor.Document);
        ApplyThemeBrushes();
    }

    private void OnFoldingTimerTick(object? sender, EventArgs e)
    {
        _foldingTimer.Stop();
        if (_foldingManager is not null)
        {
            TmFoldingStrategy.UpdateFoldings(_foldingManager, Editor.Document);
        }
    }

    private void OnCaretPositionChanged(object? sender, EventArgs e)
    {
        CaretLine = Editor.TextArea.Caret.Line;
        CaretColumn = Editor.TextArea.Caret.Column;
    }

    // Los pinceles de tema llegan por DynamicResource: al cambiar de tema WPF
    // actualiza estas propiedades y aquí se reaplican, porque en AvalonEdit no son
    // enlazables. El redibujado recoge además los colores de sintaxis y de
    // diagnósticos, que se resuelven en cada pintado.
    private static void OnThemeBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (CodeEditor)d;
        editor.ApplyThemeBrushes();
        editor.Editor.TextArea.TextView.Redraw();
    }

    private void ApplyThemeBrushes()
    {
        // Mientras el control no está en el árbol visual los recursos aún no se
        // han resuelto (pinceles nulos): se aplica lo que haya disponible.
        if (SelectionAccentBrush is { } accent)
        {
            EditorThemeBrushes.ApplySelection(Editor.TextArea, accent);
            _searchPanel.MarkerBrush = EditorThemeBrushes.WithOpacity(accent, SearchMarkerOpacity);
        }

        if (CaretBrush is { } caret)
        {
            Editor.TextArea.Caret.CaretBrush = caret;
        }

        // Margen de plegado y texto de un bloque plegado.
        if (FoldingMarkerBrush is { } marker
            && SelectedFoldingMarkerBrush is { } selectedMarker
            && FoldingMarkerBackgroundBrush is { } markerBackground)
        {
            FoldingElementGenerator.TextBrush = marker;
            foreach (var margin in Editor.TextArea.LeftMargins.OfType<FoldingMargin>())
            {
                margin.FoldingMarkerBrush = marker;
                margin.FoldingMarkerBackgroundBrush = markerBackground;
                margin.SelectedFoldingMarkerBrush = selectedMarker;
                margin.SelectedFoldingMarkerBackgroundBrush = markerBackground;
            }
        }
    }
}
