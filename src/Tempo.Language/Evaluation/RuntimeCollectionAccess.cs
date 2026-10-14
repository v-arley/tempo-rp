namespace Tempo.Language.Evaluation;

/// <summary>
/// Operaciones comunes de acceso, modificación y longitud
/// para sequence y score.
/// </summary>
internal static class RuntimeCollectionAccess
{
    public static int ReadIndex(
        RuntimeValue indexValue)
    {
        ArgumentNullException.ThrowIfNull(indexValue);

        if (indexValue.Kind != RuntimeValueKind.Sample)
        {
            throw new InvalidOperationException(
                "El índice de una colección TM debe ser de tipo sample.");
        }

        return (int)indexValue.Value!;
    }

    public static RuntimeValue Read(
        RuntimeValue target,
        int index,
        int line)
    {
        ArgumentNullException.ThrowIfNull(target);

        switch (target.Kind)
        {
            case RuntimeValueKind.Sequence:
            {
                RuntimeSequence sequence =
                    (RuntimeSequence)target.Value!;

                EnsureInRange(
                    index,
                    sequence.Count,
                    line);

                return sequence.Get(index);
            }

            case RuntimeValueKind.Score:
            {
                RuntimeScore score =
                    (RuntimeScore)target.Value!;

                EnsureInRange(
                    index,
                    score.RowCount,
                    line);

                return RuntimeValue.Sequence(
                    score.GetRow(index));
            }

            default:
                throw new InvalidOperationException(
                    $"El valor '{target.Kind}' no admite indexación.");
        }
    }

    public static void Write(
        RuntimeValue target,
        int index,
        RuntimeValue value,
        int line)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(value);

        switch (target.Kind)
        {
            case RuntimeValueKind.Sequence:
            {
                RuntimeSequence sequence =
                    (RuntimeSequence)target.Value!;

                EnsureInRange(
                    index,
                    sequence.Count,
                    line);

                sequence.Set(
                    index,
                    value);

                return;
            }

            case RuntimeValueKind.Score:
            {
                RuntimeScore score =
                    (RuntimeScore)target.Value!;

                EnsureInRange(
                    index,
                    score.RowCount,
                    line);

                if (value.Kind != RuntimeValueKind.Sequence)
                {
                    throw new InvalidOperationException(
                        "La asignación directa sobre una fila de score " +
                        "requiere un valor sequence.");
                }

                score.SetRow(
                    index,
                    (RuntimeSequence)value.Value!);

                return;
            }

            default:
                throw new InvalidOperationException(
                    $"El valor '{target.Kind}' no admite " +
                    "asignación indexada.");
        }
    }

    public static RuntimeValue Length(
        RuntimeValue target)
    {
        ArgumentNullException.ThrowIfNull(target);

        return target.Kind switch
        {
            RuntimeValueKind.Sequence =>
                RuntimeValue.Sample(
                    ((RuntimeSequence)target.Value!).Count),

            RuntimeValueKind.Score =>
                RuntimeValue.Sample(
                    ((RuntimeScore)target.Value!).RowCount),

            _ => throw new InvalidOperationException(
                $"El valor '{target.Kind}' no posee length().")
        };
    }

    private static void EnsureInRange(
        int index,
        int count,
        int line)
    {
        if (index >= 0 && index < count)
        {
            return;
        }

        throw new TmRuntimeException(
            RuntimeErrorCodes.IndexOutOfBounds,
            line,
            $"El índice {index} está fuera de los límites " +
            $"de la colección.");
    }
}