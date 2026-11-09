using Tempo.Language.Syntax;

namespace Tempo.Language.Outline;

/// <summary>Naturaleza de un nombre declarado dentro de una función o de <c>session</c>.</summary>
public enum LocalKind
{
    Variable,
    Constant,
    Parameter
}

/// <summary>Un nombre local: variable, constante, parámetro o variable de <c>loop</c>.</summary>
public sealed record LocalOutline(string Name, string TypeName, LocalKind Kind, int Line);

/// <summary>La firma de una función tal como se escribe en TM.</summary>
public sealed record FunctionOutline(
    string Name,
    string ReturnType,
    IReadOnlyList<LocalOutline> Parameters,
    string FileName,
    int Line)
{
    /// <summary>Ejemplo: <c>sample : sumar(sample a, sample b)</c>.</summary>
    public string Signature =>
        $"{ReturnType} : {Name}({string.Join(", ", Parameters.Select(parameter => $"{parameter.TypeName} {parameter.Name}"))})";
}

/// <summary>
/// Tramo de líneas que ocupa una función o <c>session</c>, con los nombres que
/// declara. El AST no guarda dónde termina un bloque: cada tramo llega hasta la
/// línea anterior al siguiente.
/// </summary>
public sealed record RegionOutline(int StartLine, int EndLine, IReadOnlyList<LocalOutline> Locals)
{
    public bool Contains(int line) => line >= StartLine && line <= EndLine;
}

/// <summary>
/// Lo que un editor necesita saber de un archivo para sugerir nombres: las
/// funciones que puede invocar (las suyas y las importadas, §6.5) y los
/// nombres locales de cada función. Es una foto de solo lectura del último
/// análisis que se pudo construir; no conoce los diagnósticos.
/// </summary>

public sealed class FileOutline
{
    private FileOutline(IReadOnlyList<FunctionOutline> functions, IReadOnlyList<RegionOutline> regions)
    {
        Functions = functions;
        Regions = regions;
    }

    public static FileOutline Empty { get; } = new(Array.Empty<FunctionOutline>(), Array.Empty<RegionOutline>());

    /// <summary>Funciones invocables desde el archivo, propias e importadas.</summary>
    public IReadOnlyList<FunctionOutline> Functions { get; }

    public IReadOnlyList<RegionOutline> Regions { get; }

    /// <summary>
    /// Nombres locales visibles en <paramref name="line"/>: los parámetros y las
    /// declaraciones anteriores de la función o <c>session</c> que la contiene.
    /// Es una aproximación por líneas: no distingue bloques internos ya cerrados.
    /// </summary>
    public IEnumerable<LocalOutline> LocalsVisibleAt(int line) =>
        Regions
            .Where(region => region.Contains(line))
            .SelectMany(region => region.Locals)
            .Where(local => local.Kind == LocalKind.Parameter || local.Line <= line);

    /// <summary>
    /// Construye el resumen de <paramref name="file"/>, que puede ver las
    /// funciones de <paramref name="visibleFiles"/>. Un archivo sin AST produce
    /// <see cref="Empty"/>.
    /// </summary>
    public static FileOutline Of(SyntaxTree file, IEnumerable<SyntaxTree> visibleFiles)
    {
        if (file is not { IsParsed: true })
        {
            return Empty;
        }

        List<FunctionOutline> functions = visibleFiles
            .Where(visible => visible.IsParsed)
            .Append(file)
            .SelectMany(source => source.Root.Functions.Select(function => FunctionOf(function, source.FileName)))
            .DistinctBy(function => function.Name)
            .ToList();

        return new FileOutline(functions, RegionsOf(file.Root));
    }

    private static FunctionOutline FunctionOf(FunctionDeclarationNode function, string fileName) =>
        new(function.Identifier, function.ReturnType, ParametersOf(function), fileName, function.Line);

    private static List<LocalOutline> ParametersOf(FunctionDeclarationNode function) =>
        (function.Parameters ?? Array.Empty<ParameterNode>())
            .Select(parameter => new LocalOutline(parameter.Identifier, parameter.TypeName, LocalKind.Parameter, parameter.Line))
            .ToList();

    // Cada función y la session son un tramo; se ordenan por línea y cada uno
    // termina donde empieza el siguiente.
    private static List<RegionOutline> RegionsOf(ProgramNode program)
    {
        var starts = program.Functions
            .Select(function => (Line: function.Line, Locals: ParametersOf(function).Concat(LocalDeclarations.In(function.Body)).ToList()))
            .ToList();

        if (program.Session is { } session)
        {
            starts.Add((session.Line, LocalDeclarations.In(session.Body).ToList()));
        }

        starts.Sort((left, right) => left.Line.CompareTo(right.Line));

        return starts
            .Select((start, index) => new RegionOutline(
                start.Line,
                index + 1 < starts.Count ? starts[index + 1].Line - 1 : int.MaxValue,
                start.Locals))
            .ToList();
    }
}
