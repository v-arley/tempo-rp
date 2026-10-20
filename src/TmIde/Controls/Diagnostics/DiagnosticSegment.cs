using ICSharpCode.AvalonEdit.Document;

using TmIde.Services.Analysis;

namespace TmIde.Controls.Diagnostics;

/// <summary>Rango de un diagnóstico dentro del documento. Al ser un
/// <see cref="TextSegment"/>, AvalonEdit lo desplaza solo mientras se escribe.</summary>

public sealed class DiagnosticSegment : TextSegment
{
    public DiagnosticSegment(EditorDiagnostic diagnostic, int start, int length)
    {
        Diagnostic = diagnostic;
        StartOffset = start;
        Length = length;
    }

    public EditorDiagnostic Diagnostic { get; }
}
