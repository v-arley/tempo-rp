using System;

namespace TmIde.Controls.Syntax;

/// <summary>
/// Delimitadores de bloque de TM (gramática del Parser:
/// <c>block = "&gt;", {statement}, "&lt;"</c>). Como <c>&gt;</c> y <c>&lt;</c> son
/// también operadores relacionales y <c>&gt;&gt;</c> / <c>&lt;&lt;</c> son operadores
/// de flujo, solo cuentan como delimitador si <c>&gt;</c> es lo último de código en
/// su línea y <c>&lt;</c> lo primero. Fuente única de esta regla para el plegado,
/// la sangría y el editor.
/// </summary>

public static class TmBlockDelimiters
{
    public const char Open = '>';
    public const char Close = '<';

    /// <summary>El código de la línea (sin espacios ni comentarios al final) termina abriendo un bloque.</summary>
    public static bool OpensBlock(ReadOnlySpan<char> lineCode)
    {
        var trimmed = lineCode.TrimEnd();
        return trimmed.EndsWith(">", StringComparison.Ordinal) && !trimmed.EndsWith(">>", StringComparison.Ordinal);
    }

    /// <summary>El código de la línea (sin espacios al inicio) empieza cerrando un bloque.</summary>
    public static bool ClosesBlock(ReadOnlySpan<char> lineCode)
    {
        var trimmed = lineCode.TrimStart();
        return trimmed.StartsWith("<", StringComparison.Ordinal) && !trimmed.StartsWith("<<", StringComparison.Ordinal);
    }
}
