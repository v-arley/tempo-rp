using Tempo.Language.Binding;
using Tempo.Language.Symbols;
using Tempo.Language.Syntax;
using Xunit;

namespace Tempo.Language.Tests.Binding;

/// <summary>
/// Cada fila de la tabla de operadores de la §4.4.2, más las combinaciones que
/// la especificación deja explícitamente fuera.
/// </summary>

public sealed class OperatorRuleTableTests
{
    [Theory]
    // Aritmética entera y flotante.
    [InlineData(TokenType.Plus, TypeKind.Sample, TypeKind.Sample, TypeKind.Sample)]
    [InlineData(TokenType.Plus, TypeKind.Wave, TypeKind.Wave, TypeKind.Wave)]
    [InlineData(TokenType.Minus, TypeKind.Sample, TypeKind.Sample, TypeKind.Sample)]
    [InlineData(TokenType.Minus, TypeKind.Wave, TypeKind.Wave, TypeKind.Wave)]
    [InlineData(TokenType.Multiply, TypeKind.Sample, TypeKind.Sample, TypeKind.Sample)]
    [InlineData(TokenType.Multiply, TypeKind.Wave, TypeKind.Wave, TypeKind.Wave)]
    [InlineData(TokenType.Divide, TypeKind.Sample, TypeKind.Sample, TypeKind.Sample)]
    [InlineData(TokenType.Divide, TypeKind.Wave, TypeKind.Wave, TypeKind.Wave)]
    // Concatenación, en ambos órdenes.
    [InlineData(TokenType.Plus, TypeKind.Phrase, TypeKind.Phrase, TypeKind.Phrase)]
    [InlineData(TokenType.Plus, TypeKind.Phrase, TypeKind.Sample, TypeKind.Phrase)]
    [InlineData(TokenType.Plus, TypeKind.Sample, TypeKind.Phrase, TypeKind.Phrase)]
    [InlineData(TokenType.Plus, TypeKind.Phrase, TypeKind.Signal, TypeKind.Phrase)]
    [InlineData(TokenType.Plus, TypeKind.Tone, TypeKind.Phrase, TypeKind.Phrase)]
    // Igualdad sobre el mismo tipo primitivo.
    [InlineData(TokenType.Equal, TypeKind.Sample, TypeKind.Sample, TypeKind.Signal)]
    [InlineData(TokenType.Equal, TypeKind.Phrase, TypeKind.Phrase, TypeKind.Signal)]
    [InlineData(TokenType.NotEqual, TypeKind.Tone, TypeKind.Tone, TypeKind.Signal)]
    [InlineData(TokenType.NotEqual, TypeKind.Signal, TypeKind.Signal, TypeKind.Signal)]
    // Relacionales, solo numéricos.
    [InlineData(TokenType.GreaterThan, TypeKind.Sample, TypeKind.Sample, TypeKind.Signal)]
    [InlineData(TokenType.LessThan, TypeKind.Wave, TypeKind.Wave, TypeKind.Signal)]
    [InlineData(TokenType.GreaterOrEqual, TypeKind.Sample, TypeKind.Sample, TypeKind.Signal)]
    [InlineData(TokenType.LessOrEqual, TypeKind.Wave, TypeKind.Wave, TypeKind.Signal)]
    // Lógicos.
    [InlineData(TokenType.And, TypeKind.Signal, TypeKind.Signal, TypeKind.Signal)]
    [InlineData(TokenType.Or, TypeKind.Signal, TypeKind.Signal, TypeKind.Signal)]
    public void TryResolveBinary_DefinedCombination_ReturnsSpecifiedResult(
        TokenType op,
        TypeKind left,
        TypeKind right,
        TypeKind expected)
    {
        Assert.True(
            OperatorRuleTable.TryResolveBinary(op, Of(left), Of(right), out TmType result));

        Assert.Equal(expected, result.Kind);
    }

    [Theory]
    // Mezcla de tipos numéricos distintos.
    [InlineData(TokenType.Plus, TypeKind.Sample, TypeKind.Wave)]
    [InlineData(TokenType.Multiply, TypeKind.Wave, TypeKind.Sample)]
    // Aritmética no definida sobre tone o signal.
    [InlineData(TokenType.Plus, TypeKind.Tone, TypeKind.Tone)]
    [InlineData(TokenType.Minus, TypeKind.Phrase, TypeKind.Phrase)]
    [InlineData(TokenType.Multiply, TypeKind.Signal, TypeKind.Signal)]
    // Relacionales sobre tipos no numéricos.
    [InlineData(TokenType.GreaterThan, TypeKind.Phrase, TypeKind.Phrase)]
    [InlineData(TokenType.LessThan, TypeKind.Tone, TypeKind.Tone)]
    // Igualdad entre tipos primitivos distintos.
    [InlineData(TokenType.Equal, TypeKind.Sample, TypeKind.Phrase)]
    // Lógicos sobre no booleanos.
    [InlineData(TokenType.And, TypeKind.Sample, TypeKind.Sample)]
    public void TryResolveBinary_UndefinedCombination_ReturnsFalse(
        TokenType op,
        TypeKind left,
        TypeKind right)
    {
        Assert.False(OperatorRuleTable.TryResolveBinary(op, Of(left), Of(right), out _));
    }

    [Theory]
    [InlineData(TokenType.Equal)]
    [InlineData(TokenType.NotEqual)]
    [InlineData(TokenType.Plus)]
    public void TryResolveBinary_CompositeOperand_ReturnsFalse(TokenType op)
    {
        TmType sequence = TmType.SequenceOf(TmType.Sample);

        Assert.False(OperatorRuleTable.TryResolveBinary(op, sequence, sequence, out _));
    }

    [Fact]
    public void TryResolveBinary_UnknownOperand_PropagatesUnknownWithoutFailing()
    {
        Assert.True(
            OperatorRuleTable.TryResolveBinary(
                TokenType.Plus,
                TmType.Unknown,
                TmType.Phrase,
                out TmType result));

        Assert.True(result.IsUnknown);
    }

    [Theory]
    [InlineData(TokenType.Not, TypeKind.Signal, TypeKind.Signal)]
    [InlineData(TokenType.Minus, TypeKind.Sample, TypeKind.Sample)]
    [InlineData(TokenType.Minus, TypeKind.Wave, TypeKind.Wave)]
    public void TryResolveUnary_DefinedOperand_ReturnsSpecifiedResult(
        TokenType op,
        TypeKind operand,
        TypeKind expected)
    {
        Assert.True(OperatorRuleTable.TryResolveUnary(op, Of(operand), out TmType result));
        Assert.Equal(expected, result.Kind);
    }

    [Theory]
    [InlineData(TokenType.Not, TypeKind.Sample)]
    [InlineData(TokenType.Minus, TypeKind.Phrase)]
    [InlineData(TokenType.Minus, TypeKind.Signal)]
    public void TryResolveUnary_UndefinedOperand_ReturnsFalse(TokenType op, TypeKind operand)
    {
        Assert.False(OperatorRuleTable.TryResolveUnary(op, Of(operand), out _));
    }

    [Fact]
    public void RequiresPhraseConcatenationWarning_MixedOperands_IsTrueInBothOrders()
    {
        Assert.True(
            OperatorRuleTable.RequiresPhraseConcatenationWarning(
                TokenType.Plus,
                TmType.Phrase,
                TmType.Sample));

        Assert.True(
            OperatorRuleTable.RequiresPhraseConcatenationWarning(
                TokenType.Plus,
                TmType.Sample,
                TmType.Phrase));
    }

    [Fact]
    public void RequiresPhraseConcatenationWarning_TwoPhrases_IsFalse()
    {
        // No hay conversión que advertir: es concatenación pura.
        Assert.False(
            OperatorRuleTable.RequiresPhraseConcatenationWarning(
                TokenType.Plus,
                TmType.Phrase,
                TmType.Phrase));
    }

    private static TmType Of(TypeKind kind)
    {
        return kind switch
        {
            TypeKind.Sample => TmType.Sample,
            TypeKind.Wave => TmType.Wave,
            TypeKind.Tone => TmType.Tone,
            TypeKind.Phrase => TmType.Phrase,
            TypeKind.Signal => TmType.Signal,
            TypeKind.Silence => TmType.Silence,
            TypeKind.Void => TmType.Void,
            _ => TmType.Unknown
        };
    }
}
