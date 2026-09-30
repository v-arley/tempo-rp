using TmCompiler.Domain.Syntactic;
using TmInterpreter.Domain;

namespace TmInterpreter.Application;

/// <summary>
/// Intérprete principal de un programa TM previamente validado.
/// </summary>
public sealed class Interpreter : IInterpreter
{
    private readonly ProgramNode _program;
    private readonly StatementExecutor _statements;

    public Interpreter(
        ProgramNode program,
        IRuntimeInput input,
        IRuntimeOutput output)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        _program = program;

        var registry =
            new FunctionRegistry(
                program.Functions);

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

        _statements.ExecuteBlock(
            _program.Session.Body,
            globalEnvironment);
    }
}