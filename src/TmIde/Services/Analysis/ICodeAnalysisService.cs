using System.Collections.Generic;

using Tempo.Language.Outline;

namespace TmIde.Services.Analysis;

/// <summary>
/// Resultado de analizar un documento: lo que el editor subraya y, si el
/// archivo se pudo construir, su resumen para el autocompletado
/// (<c>null</c> cuando hay un error léxico o sintáctico).
/// </summary>
public sealed record DocumentAnalysis(IReadOnlyList<EditorDiagnostic> Diagnostics, FileOutline? Outline)
{
    public static DocumentAnalysis Empty { get; } = new([], null);
}

/// <summary>
/// Compila el proyecto de un documento y devuelve los diagnósticos y el
/// resumen de ese documento para el editor. Es síncrono y sin estado: quien lo
/// llama decide en qué hilo corre y cuándo descartar resultados obsoletos.
/// </summary>

public interface ICodeAnalysisService
{
    /// <param name="filePath">Ruta completa del documento analizado.</param>
    /// <param name="openDocuments">
    /// Texto de los documentos abiertos (ruta completa → texto), incluido el
    /// analizado. Sustituye al contenido en disco.
    /// </param>
    DocumentAnalysis Analyze(string filePath, IReadOnlyDictionary<string, string> openDocuments);
}
