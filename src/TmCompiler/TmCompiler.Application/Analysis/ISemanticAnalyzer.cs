namespace TmCompiler.Application.Analysis;

/// <summary>
/// Tercera fase del compilador (§12.1): resolución de nombres, verificación de
/// tipos y validación de constantes. Sigue el mismo patrón que
/// <c>ILexer</c> e <c>IParser</c>: la entrada se recibe en el constructor.
/// </summary>

public interface ISemanticAnalyzer
{
    SemanticAnalysisResult Analyze();
}
