namespace TmInterpreter.Domain;

/// <summary>
/// Códigos de error producidos durante la ejecución de TM.
/// </summary>
public static class RuntimeErrorCodes
{
    public const string DivisionByZero = "R001";
    public const string IndexOutOfBounds = "R002";
    public const string ArithmeticOverflow = "R003";
    public const string InputParseError = "R004";
    public const string UndefinedFunction = "R005";
    public const string InputEndOfFile = "R006";
    public const string InvalidToneInput = "R007";
}