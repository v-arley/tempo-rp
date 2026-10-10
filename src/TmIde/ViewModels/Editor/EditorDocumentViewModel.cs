using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ICSharpCode.AvalonEdit.Document;

using TmIde.Services.Analysis;
using TmIde.ViewModels;

namespace TmIde.ViewModels.Editor;

/// <summary>
/// Un documento abierto en el editor (una pestaña). Posee su propio
/// <see cref="TextDocument"/> de AvalonEdit: así cada pestaña conserva su pila de
/// deshacer, y el editor puede recordar cursor y desplazamiento por documento.
/// Es el único tipo del modelo de vista que conoce AvalonEdit.
/// </summary>

public sealed partial class EditorDocumentViewModel : ViewModelBase
{
    private const int AnalysisDelayMilliseconds = 400;

    private readonly Action<EditorDocumentViewModel>? _onClose;
    private readonly ICodeAnalysisService? _analysis;
    private CancellationTokenSource? _analysisCts;

    /// <summary>Verdadero cuando el texto difiere del original (derivado de la pila de deshacer).</summary>
    [ObservableProperty]
    private bool _isModified;

    [ObservableProperty]
    private int _caretLine = 1;

    [ObservableProperty]
    private int _caretColumn = 1;

    /// <summary>Resultado del último análisis; el editor lo subraya.</summary>
    [ObservableProperty]
    private IReadOnlyList<EditorDiagnostic> _diagnostics = [];

    public EditorDocumentViewModel(
        string fileName,
        string relativePath,
        string content,
        Action<EditorDocumentViewModel>? onClose = null,
        ICodeAnalysisService? analysis = null)
    {
        FileName = fileName;
        RelativePath = relativePath;
        _onClose = onClose;
        _analysis = analysis;

        Document = new TextDocument(content);
        Document.UndoStack.MarkAsOriginalFile();
        Document.TextChanged += OnDocumentTextChanged;
        Document.UndoStack.PropertyChanged += OnUndoStackPropertyChanged;

        ScheduleAnalysis(content);
    }

    public TextDocument Document { get; }

    /// <summary>Texto actual del documento. Solo desde el hilo de la UI (el documento es de ese hilo).</summary>
    public string Content => Document.Text;

    public int ErrorCount => Diagnostics.Count(d => d.Severity == EditorDiagnosticSeverity.Error);

    public int WarningCount => Diagnostics.Count - ErrorCount;

    public string FileName { get; }

    /// <summary>Ruta relativa del archivo, separada por <c>/</c>, que alimenta el breadcrumb.</summary>
    public string RelativePath { get; }

    [RelayCommand]
    private void Close()
    {
        _analysisCts?.Cancel();
        _onClose?.Invoke(this);
    }

    // Se toma una copia del texto en el hilo de la UI: el análisis corre en otro
    // hilo y TextDocument no se puede leer desde allí.
    private void OnDocumentTextChanged(object? sender, EventArgs e) => ScheduleAnalysis(Document.Text);

    private void OnUndoStackPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UndoStack.IsOriginalFile))
        {
            IsModified = !Document.UndoStack.IsOriginalFile;
        }
    }

    partial void OnDiagnosticsChanged(IReadOnlyList<EditorDiagnostic> value)
    {
        OnPropertyChanged(nameof(ErrorCount));
        OnPropertyChanged(nameof(WarningCount));
    }

    // Debounce: cada cambio cancela el análisis pendiente y espera a que se
    // deje de escribir. El análisis corre fuera del hilo de la UI y un resultado
    // obsoleto (cancelado) nunca se publica.
    private void ScheduleAnalysis(string text)
    {
        if (_analysis is null)
        {
            return;
        }

        _analysisCts?.Cancel();
        var cts = new CancellationTokenSource();
        _analysisCts = cts;
        _ = RunAnalysisAsync(text, cts.Token);
    }

    private async Task RunAnalysisAsync(string text, CancellationToken token)
    {
        try
        {
            await Task.Delay(AnalysisDelayMilliseconds, token);
            var result = await Task.Run(() => _analysis!.Analyze(text, FileName), token);
            if (!token.IsCancellationRequested)
            {
                Diagnostics = result;
            }
        }
        catch (OperationCanceledException)
        {
            // Texto más reciente en camino: este resultado ya no sirve.
        }
    }
}
