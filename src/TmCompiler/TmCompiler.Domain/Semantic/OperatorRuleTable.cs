using TmCompiler.Domain.Lexical;

namespace TmCompiler.Domain.Semantic;

/// <summary>
/// Tabla de operadores de la §4.4.2 expresada como dato: añadir una regla es
/// añadir una fila, no una rama condicional. Toda combinación ausente de la
/// tabla es un error de tipo en la operación.
/// </summary>

public static class OperatorRuleTable
{
    private static readonly TypeKind[] PrimitiveKinds =
    {
        TypeKind.Sample, TypeKind.Wave, TypeKind.Tone, TypeKind.Phrase, TypeKind.Signal
    };

    private static readonly Dictionary<OperatorSignature, TypeKind> BinaryRules = BuildBinaryRules();

    private static readonly Dictionary<OperatorSignature, TypeKind> UnaryRules = BuildUnaryRules();

    /// <summary>
    /// Resuelve el tipo resultante de un operador binario. Devuelve <c>false</c>
    /// si la combinación no está definida en la especificación.
    /// </summary>
    public static bool TryResolveBinary(TokenType op, TmType left, TmType right, out TmType result)
    {
        result = TmType.Unknown;

        if (left is null || right is null)
        {
            return true;
        }

        // El tipo error se propaga en silencio: ya se reportó su origen.
        if (left.IsUnknown || right.IsUnknown)
        {
            return true;
        }

        // Ningún operador de TM admite sequence ni score (§4.4.5).
        if (left.IsComposite || right.IsComposite)
        {
            return false;
        }

        var signature = new OperatorSignature(op, left.Kind, right.Kind);

        if (BinaryRules.TryGetValue(signature, out TypeKind resultKind))
        {
            result = FromKind(resultKind);
            return true;
        }

        // Concatenación: phrase + cualquier primitivo, en ambos órdenes (§4.4.1.2).
        if (IsPhraseConcatenation(op, left, right))
        {
            result = TmType.Phrase;
            return true;
        }

        return false;
    }

    public static bool TryResolveUnary(TokenType op, TmType operand, out TmType result)
    {
        result = TmType.Unknown;

        if (operand is null || operand.IsUnknown)
        {
            return true;
        }

        if (operand.IsComposite)
        {
            return false;
        }

        var signature = new OperatorSignature(op, operand.Kind, operand.Kind);

        if (UnaryRules.TryGetValue(signature, out TypeKind resultKind))
        {
            result = FromKind(resultKind);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Indica si la operación es una concatenación con conversión textual
    /// implícita, que la especificación obliga a advertir (W004).
    /// </summary>
    public static bool RequiresPhraseConcatenationWarning(TokenType op, TmType left, TmType right)
    {
        if (left is null || right is null || left.IsUnknown || right.IsUnknown)
        {
            return false;
        }

        return IsPhraseConcatenation(op, left, right);
    }

    private static bool IsPhraseConcatenation(TokenType op, TmType left, TmType right)
    {
        if (op != TokenType.Plus)
        {
            return false;
        }

        bool leftIsPhrase = left.Kind == TypeKind.Phrase;
        bool rightIsPhrase = right.Kind == TypeKind.Phrase;

        // phrase + phrase es concatenación pura: no hay conversión que advertir.
        if (leftIsPhrase == rightIsPhrase)
        {
            return false;
        }

        return leftIsPhrase ? right.IsPrimitive : left.IsPrimitive;
    }

    private static Dictionary<OperatorSignature, TypeKind> BuildBinaryRules()
    {
        var rules = new Dictionary<OperatorSignature, TypeKind>();

        // Aritmética: sample x sample -> sample, wave x wave -> wave.
        foreach (TokenType op in new[]
                 {
                     TokenType.Plus, TokenType.Minus, TokenType.Multiply, TokenType.Divide
                 })
        {
            rules[new OperatorSignature(op, TypeKind.Sample, TypeKind.Sample)] = TypeKind.Sample;
            rules[new OperatorSignature(op, TypeKind.Wave, TypeKind.Wave)] = TypeKind.Wave;
        }

        // Concatenación de cadenas.
        rules[new OperatorSignature(TokenType.Plus, TypeKind.Phrase, TypeKind.Phrase)] =
            TypeKind.Phrase;

        // Igualdad: mismo tipo primitivo -> signal.
        foreach (TokenType op in new[] { TokenType.Equal, TokenType.NotEqual })
        {
            foreach (TypeKind kind in PrimitiveKinds)
            {
                rules[new OperatorSignature(op, kind, kind)] = TypeKind.Signal;
            }

            // Comparación con el valor nulo, coherente con "silence es asignable
            // a phrase" (§4.4.1.3).
            rules[new OperatorSignature(op, TypeKind.Phrase, TypeKind.Silence)] = TypeKind.Signal;
            rules[new OperatorSignature(op, TypeKind.Silence, TypeKind.Phrase)] = TypeKind.Signal;
            rules[new OperatorSignature(op, TypeKind.Silence, TypeKind.Silence)] = TypeKind.Signal;
        }

        // Relacionales: solo numéricos.
        foreach (TokenType op in new[]
                 {
                     TokenType.GreaterThan, TokenType.LessThan,
                     TokenType.GreaterOrEqual, TokenType.LessOrEqual
                 })
        {
            rules[new OperatorSignature(op, TypeKind.Sample, TypeKind.Sample)] = TypeKind.Signal;
            rules[new OperatorSignature(op, TypeKind.Wave, TypeKind.Wave)] = TypeKind.Signal;
        }

        // Lógicos.
        foreach (TokenType op in new[] { TokenType.And, TokenType.Or })
        {
            rules[new OperatorSignature(op, TypeKind.Signal, TypeKind.Signal)] = TypeKind.Signal;
        }

        return rules;
    }

    private static Dictionary<OperatorSignature, TypeKind> BuildUnaryRules()
    {
        return new Dictionary<OperatorSignature, TypeKind>
        {
            [new OperatorSignature(TokenType.Not, TypeKind.Signal, TypeKind.Signal)] =
                TypeKind.Signal,
            [new OperatorSignature(TokenType.Minus, TypeKind.Sample, TypeKind.Sample)] =
                TypeKind.Sample,
            [new OperatorSignature(TokenType.Minus, TypeKind.Wave, TypeKind.Wave)] =
                TypeKind.Wave
        };
    }

    private static TmType FromKind(TypeKind kind)
    {
        switch (kind)
        {
            case TypeKind.Sample:
                return TmType.Sample;

            case TypeKind.Wave:
                return TmType.Wave;

            case TypeKind.Tone:
                return TmType.Tone;

            case TypeKind.Phrase:
                return TmType.Phrase;

            case TypeKind.Signal:
                return TmType.Signal;

            case TypeKind.Silence:
                return TmType.Silence;

            default:
                return TmType.Unknown;
        }
    }
}
