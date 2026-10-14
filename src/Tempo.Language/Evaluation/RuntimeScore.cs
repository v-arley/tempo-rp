namespace Tempo.Language.Evaluation;

/// <summary>
/// Representa una matriz bidimensional mutable de TM.
/// Cada fila se mantiene como un RuntimeSequence.
/// </summary>
public sealed class RuntimeScore
{
    private readonly List<RuntimeSequence> _rows;

    public RuntimeScore(
        IEnumerable<RuntimeSequence> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        _rows = new List<RuntimeSequence>(rows);
    }

    public int RowCount => _rows.Count;

    public RuntimeSequence GetRow(int index)
    {
        return _rows[index];
    }

    public void SetRow(
        int index,
        RuntimeSequence row)
    {
        ArgumentNullException.ThrowIfNull(row);

        _rows[index] = row;
    }
}