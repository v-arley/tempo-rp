using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Tempo.Language;
using Tempo.Language.Diagnostics;

namespace TmIde.Services.Analysis;

/// <summary>
/// Analiza un documento con la misma <see cref="Compilation"/> que usa la CLI,
/// como hacen los IDE construidos sobre Roslyn: el editor no tiene un pipeline
/// propio, así que lo que subraya coincide con lo que reporta COMPILE. Los
/// documentos abiertos sustituyen al disco, de modo que se compila lo que el
/// usuario ve aunque no lo haya guardado, y los diagnósticos incluyen los del
/// proyecto (importaciones, funciones de otros archivos, W003 entre archivos).
/// </summary>

public sealed class CodeAnalysisService : ICodeAnalysisService
{
    public IReadOnlyList<EditorDiagnostic> Analyze(string filePath, IReadOnlyDictionary<string, string> openDocuments)
    {
        try
        {
            var compilation = Compilation.FromPath(ProjectPathOf(filePath), openDocuments);

            return compilation.DiagnosticsOf(filePath).Select(ToEditorDiagnostic).ToList();
        }
        catch (Exception)
        {
            // Un fallo interno del compilador no debe tumbar el editor: sin
            // diagnósticos hasta el siguiente cambio de texto.
            return [];
        }
    }

    /// <summary>
    /// Un proyecto TM es el directorio que contiene al archivo (§6.6). Si ese
    /// directorio no existe en disco (documento sin guardar), se compila el
    /// documento solo, con lo que importe.
    /// </summary>
    private static string ProjectPathOf(string filePath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));

        return Directory.Exists(directory) ? directory! : filePath;
    }

    private static EditorDiagnostic ToEditorDiagnostic(Diagnostic diagnostic) => new(
        diagnostic.Severity == DiagnosticSeverity.Warning ? EditorDiagnosticSeverity.Warning : EditorDiagnosticSeverity.Error,
        diagnostic.Code,
        diagnostic.Title,
        diagnostic.Description,
        diagnostic.Suggestion,
        diagnostic.Line,
        diagnostic.Column);
}
