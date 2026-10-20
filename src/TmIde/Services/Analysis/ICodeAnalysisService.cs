using System.Collections.Generic;

namespace TmIde.Services.Analysis;

/// <summary>
/// Compila el proyecto de un documento y devuelve los diagnósticos de ese
/// documento para el editor. Es síncrono y sin estado: quien lo llama decide en
/// qué hilo corre y cuándo descartar resultados obsoletos.
/// </summary>

public interface ICodeAnalysisService
{
    /// <param name="filePath">Ruta completa del documento analizado.</param>
    /// <param name="openDocuments">
    /// Texto de los documentos abiertos (ruta completa → texto), incluido el
    /// analizado. Sustituye al contenido en disco.
    /// </param>
    IReadOnlyList<EditorDiagnostic> Analyze(string filePath, IReadOnlyDictionary<string, string> openDocuments);
}
