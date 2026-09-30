using TmInterpreter.Domain;

namespace TmInterpreter.Application;

/// <summary>
/// Señal interna utilizada para abandonar una función
/// al ejecutar una sentencia ret.
/// No representa un error de runtime.
/// </summary>
internal sealed class ReturnSignal : Exception
{
    public ReturnSignal(RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        Value = value;
    }

    public RuntimeValue Value { get; }
}