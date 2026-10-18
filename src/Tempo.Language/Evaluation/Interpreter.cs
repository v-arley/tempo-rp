using Tempo.Language.Syntax;

namespace Tempo.Language.Evaluation;

/// <summary>
/// Intérprete principal de un programa TM previamente validado.
/// </summary>
public sealed class Interpreter : IInterpreter
{
    private readonly ProgramNode _program;
    private readonly StatementExecutor _statements;
    private readonly string? _entryFileName;

    public Interpreter(
        ProgramNode program,
        IRuntimeInput input,
        IRuntimeOutput output,
        IReadOnlyDictionary<string, string>? sourceFiles = null,
        string? entryFileName = null)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        _program = program;
        _entryFileName = entryFileName;

        var registry =
            new FunctionRegistry(
                program.Functions,
                sourceFiles);

        var functions =
            new FunctionInvoker(
                registry,
                input,
                output);

        _statements = functions.Statements;
    }

    public void Execute()
    {
        if (_program.Session is null)
        {
            throw new InvalidOperationException(
                "No se puede ejecutar un programa TM sin session.");
        }

        var globalEnvironment =
            new RuntimeEnvironment();

        try
        {
            _statements.ExecuteBlock(
                _program.Session.Body,
                globalEnvironment);
        }
        catch (TmRuntimeException ex)
        {
            ex.SetFileNameIfMissing(_entryFileName);
            throw;
        }
    }
}