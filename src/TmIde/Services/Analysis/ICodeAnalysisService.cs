using System.Collections.Generic;

namespace TmIde.Services.Analysis;

/// <summary>
/// Ejecuta el pipeline del compilador sobre un texto y devuelve los
/// diagnósticos para el editor. Es síncrono y sin estado: quien lo llama decide
/// en qué hilo corre y cuándo descartar resultados obsoletos.
/// </summary>

public interface ICodeAnalysisService
{
    IReadOnlyList<EditorDiagnostic> Analyze(string text, string fileName);
}
