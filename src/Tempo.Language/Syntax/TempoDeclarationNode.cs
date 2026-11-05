namespace Tempo.Language.Syntax;

/// <summary>
/// Representa la declaración del ritmo, en BPM, con el que late el panel de partículas.
/// El valor se conserva como lexema: el rango 20–300 BPM lo comprueba el
/// análisis semántico (E040), y así un literal que no cabe en un entero
/// también se reporta como fuera de rango.
///
/// Ejemplo:
/// tempo 96;
/// </summary>
public sealed record TempoDeclarationNode(
    string Value,
    int Line,
    int Column
) : AstNode;
