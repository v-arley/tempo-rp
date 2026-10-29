using System;
using Tempo.Language.Diagnostics;

namespace TmIde.ViewModels.Panel;

/// Categorias visibles de los diagnosticos
public enum DiagnosticCategory
{
    Importacion,
    Lexico,
    Sintaxis,
    Semantica,
    Ejecucion,
    Advertencia,
    Otro
}

/// Clasifica diagnosticos utilizando sus codigos oficiales.
/// No depende del titulo ni de la descripcion del error.
public static class DiagnosticCategoryClassifier
{
    public static DiagnosticCategory Classify(
        Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        var code = diagnostic.Code.ToUpperInvariant();

        // Las advertencias tienen prioridad
        if (diagnostic.Severity == DiagnosticSeverity.Warning ||
            code.StartsWith('W'))
        {
            return DiagnosticCategory.Advertencia;
        }

        // Errores especificos de importaciones
        if (code is DiagnosticCodes.ImportCycle
            or DiagnosticCodes.ImportNotFound)
        {
            return DiagnosticCategory.Importacion;
        }

        // Errores del interprete
        if (code.Length == 4 &&
            code[0] == 'R' &&
            int.TryParse(code.AsSpan(1), out _))
        {
            return DiagnosticCategory.Ejecucion;
        }

        // Codigos E001 - E039
        if (code.Length == 4 &&
            code[0] == 'E' &&
            int.TryParse(code.AsSpan(1), out var number))
        {
            if (number is >= 21 and <= 26)
            {
                return DiagnosticCategory.Lexico;
            }

            if (number is >= 27 and <= 39)
            {
                return DiagnosticCategory.Sintaxis;
            }

            if (number is >= 1 and <= 20)
            {
                return DiagnosticCategory.Semantica;
            }
        }

        return DiagnosticCategory.Otro;
    }

    /// Devuelve el encabezado visible en Output
    public static string GetHeading(
        DiagnosticCategory category)
    {
        return category switch
        {
            DiagnosticCategory.Importacion =>
                "── IMPORTACIÓN ──",

            DiagnosticCategory.Lexico =>
                "── ANÁLISIS LÉXICO ──",

            DiagnosticCategory.Sintaxis =>
                "── ANÁLISIS SINTÁCTICO ──",

            DiagnosticCategory.Semantica =>
                "── ANÁLISIS SEMÁNTICO ──",

            DiagnosticCategory.Ejecucion =>
                "── EJECUCIÓN ──",

            DiagnosticCategory.Advertencia =>
                "── ADVERTENCIAS ──",

            _ => "── OTROS DIAGNÓSTICOS ──"
        };
    }
}
