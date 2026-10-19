using Tempo.Language.Diagnostics;
using Tempo.Language.Syntax;
using Xunit;

namespace Tempo.Language.Tests.Syntax;

public sealed class SyntacticDiagnosticCodeTests
{
    [Theory]

    // E027 - Falta punto y coma
    [InlineData(
        "session >\n    output >> 1\n<",
        DiagnosticCodes.MissingSemicolon,
        2)]

    // E028 - Delimitador de bloque incorrecto
    [InlineData(
        "session <\n    output >> 1;\n>",
        DiagnosticCodes.MissingBlockDelimiter,
        1)]

    // E029 - Condicion sin corchetes
    [InlineData(
        "session >\n    if true >\n        output >> 1;\n    <\n<",
        DiagnosticCodes.MissingConditionBrackets,
        2)]

    // E030 - Funcion sin tipo de retorno
    [InlineData(
        "compose funcion() >\n    ret;\n<",
        DiagnosticCodes.MalformedFunctionDeclaration,
        1)]

    // E031 - Parametro sin nombre
    [InlineData(
        "sample : compose sumar(sample) >\n    ret 1;\n<",
        DiagnosticCodes.MalformedParameterList,
        1)]

    // E032 - Se esperaba una expresion
    [InlineData(
        "session >\n    output >> ;\n<",
        DiagnosticCodes.ExpectedExpression,
        2)]

    // E033 - Agrupacion sin cerrar
    [InlineData(
        "session >\n    output >> (1 + 2;\n<",
        DiagnosticCodes.UnclosedGrouping,
        2)]

    // E034 - Declaracion de variable mal formada
    [InlineData(
        "session >\n    sample = 10;\n<",
        DiagnosticCodes.MalformedVariableDeclaration,
        2)]

    // E035 - Sentencia de entrada/salida mal formada
    [InlineData(
        "session >\n    input >> dato;\n<",
        DiagnosticCodes.MalformedInputOutput,
        2)]

    // E036 - Cabecera de loop mal formada
    [InlineData(
        "session >\n    loop [true] >\n    <\n<",
        DiagnosticCodes.MalformedLoopHeader,
        2)]

    // E037 - Sentencia mix mal formada: default duplicado
    [InlineData(
        "session >\n    mix 1 >\n        default: > <\n        default: > <\n    <\n<",
        DiagnosticCodes.MalformedMixStatement,
        4)]

    // E038 - Sentencia no valida
    [InlineData(
        "session >\n    else > <\n<",
        DiagnosticCodes.InvalidStatement,
        2)]

    // E039 - Elemento inesperado en el nivel superior
    [InlineData(
        "output >> 1;",
        DiagnosticCodes.UnexpectedTopLevelElement,
        1)]

    public void InvalidSyntax_ReportsExpectedCodeAndLine(
        string source,
        string expectedCode,
        int expectedLine)
    {
        SyntaxTree syntaxTree = SyntaxTree.Parse(
            source,
            "test.tmpr",
            "test.tmpr");

        Diagnostic diagnostic =
            Assert.Single(syntaxTree.Diagnostics);

        Assert.Equal(expectedCode, diagnostic.Code);
        Assert.Equal(expectedLine, diagnostic.Line);
        Assert.Equal("test.tmpr", diagnostic.FileName);
        Assert.True(diagnostic.IsError);
    }
}