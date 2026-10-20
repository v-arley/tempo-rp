namespace TmIde.Controls.Syntax;

/// <summary>Fragmento coloreable: posición absoluta en el documento y categoría.</summary>
public readonly record struct TmSyntaxSpan(int Start, int Length, TmSyntaxKind Kind)
{
    public int End => Start + Length;

    public bool Contains(int position) => position >= Start && position < End;
}
