
using System;
using System.Collections.Generic;
using System.IO;

using Tempo.Language.Diagnostics;

namespace TmIde.ViewModels.Panel;

/// <summary>
/// Panel Output: muestra mensajes del IDE y diagnósticos
/// del compilador, permitiendo navegar a sus archivos.
/// </summary>
public sealed class OutputPaneViewModel : BottomPaneViewModel
{
    private readonly Dictionary<int, DiagnosticNavigationTarget>
        _diagnosticLocations = new();

    /// Se dispara cuando el usuario activa un diagnóstico.
    public event Action<DiagnosticNavigationTarget>?
        DiagnosticActivated;

    public OutputPaneViewModel(string header)
        : base(header)
    {
    }

    /// Escribe un diagnóstico y asocia sus líneas
    /// con el archivo que lo produjo.
    public void AppendDiagnostic(
        Diagnostic diagnostic,
        string? filePath)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        var firstLine = Lines.Count + 1;

        // Conservar el formato oficial de Tempo, evitando una línea vacía al final
        var formatted = diagnostic
            .ToStandardFormat()
            .TrimEnd('\r', '\n');

        AppendLine(formatted);

        // Un diagnóstico de proyecto puede no tener un archivo físico asociado.
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var target = new DiagnosticNavigationTarget(
            Path.GetFullPath(filePath),
            Math.Max(1, diagnostic.Line),
            diagnostic.Column);

        // Todas las líneas del bloque permiten navegar, no solamente la cabecera [ERROR E###].
        for (var line = firstLine; line <= Lines.Count; line++)
        {
            _diagnosticLocations[line] = target;
        }
    }

    /// <summary>
    /// Activa un diagnóstico a partir de su número
    /// de línea visible en Output.
    /// </summary>
    public bool ActivateLine(int outputLine)
    {
        if (!_diagnosticLocations.TryGetValue(
                outputLine,
                out var target))
        {
            return false;
        }

        DiagnosticActivated?.Invoke(target);

        return true;
    }

    /// <summary>
    /// Invalida las referencias del proyecto anterior.
    /// Conserva el historial visible de Output.
    /// </summary>
    public void ClearDiagnosticLinks()
    {
        _diagnosticLocations.Clear();
    }
}
