using System;
using System.Collections.Generic;
using System.IO;
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
    private readonly ICodeAnalysisService? _analysisService;
    private readonly Func<IReadOnlyDictionary<string, string>>? _openDocumentTexts;

    // Análisis en espera o en curso; se cancela cuando llega un texto más reciente.
    private CancellationTokenSource? _pendingAnalysis;

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
        ICodeAnalysisService? analysis = null,
        string? filePath = null,
        Func<IReadOnlyDictionary<string, string>>? openDocumentTexts = null)
    {
        FileName = fileName;
        RelativePath = relativePath;
        FilePath = Path.GetFullPath(filePath ?? relativePath);
        _onClose = onClose;
        _analysisService = analysis;
        _openDocumentTexts = openDocumentTexts;

        Document = new TextDocument(content);
        Document.UndoStack.MarkAsOriginalFile();
        Document.TextChanged += OnDocumentTextChanged;
        Document.UndoStack.PropertyChanged += OnUndoStackPropertyChanged;

        ScheduleAnalysis(content);
    }

    public TextDocument Document { get; }

    /// <summary>Texto actual del documento. Solo desde el hilo de la UI (el documento es de ese hilo).</summary>
    public string Content => Document.Text;

    public int ErrorCount => Diagnostics.Count(diagnostic => diagnostic.Severity == EditorDiagnosticSeverity.Error);

    public int WarningCount => Diagnostics.Count(diagnostic => diagnostic.Severity == EditorDiagnosticSeverity.Warning);

    public string FileName { get; }

    /// <summary>Ruta relativa del archivo, separada por <c>/</c>, que alimenta el breadcrumb.</summary>
    public string RelativePath { get; }

    /// <summary>Ruta completa del archivo; identifica al documento ante el compilador.</summary>
    public string FilePath { get; }

    /// <summary>
    /// Escribe el texto en <see cref="FilePath"/> y lo marca como guardado.
    /// Lanza <see cref="IOException"/> si no se puede escribir.
    /// </summary>
    public void Save()
    {
        File.WriteAllText(FilePath, Document.Text);
        Document.UndoStack.MarkAsOriginalFile();
    }

    [RelayCommand]
    private void Close()
    {
        _pendingAnalysis?.Cancel();
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
        if (_analysisService is not { } analysisService)
        {
            return;
        }

        _pendingAnalysis?.Cancel();
        var analysis = new CancellationTokenSource();
        _pendingAnalysis = analysis;
        _ = RunAnalysisAsync(analysisService, SnapshotOpenDocuments(text), analysis.Token);
    }

    // Copia, en el hilo de la UI, el texto de todos los documentos abiertos, con
    // el de este documento en su versión más reciente.
    private Dictionary<string, string> SnapshotOpenDocuments(string text)
    {
        var snapshot = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (path, content) in _openDocumentTexts?.Invoke() ?? new Dictionary<string, string>())
        {
            snapshot[path] = content;
        }

        snapshot[FilePath] = text;
        return snapshot;
    }

    private async Task RunAnalysisAsync(
        ICodeAnalysisService analysisService,
        IReadOnlyDictionary<string, string> openDocuments,
        CancellationToken token)
    {
        try
        {
            await Task.Delay(AnalysisDelayMilliseconds, token);
            var result = await Task.Run(() => analysisService.Analyze(FilePath, openDocuments), token);
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
