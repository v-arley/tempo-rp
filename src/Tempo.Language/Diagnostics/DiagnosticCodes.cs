namespace Tempo.Language.Diagnostics;

/// <summary>
/// Códigos de diagnóstico del catálogo de la Sección 7 de la especificación
/// TM v1.1. Se exponen como constantes con nombre para evitar literales
/// dispersos por el analizador.
/// </summary>

public static class DiagnosticCodes
{
    // ---- Errores de compilación (§7.2) ----
    public const string UndeclaredIdentifier = "E001";
    public const string Redeclaration = "E002";
    public const string IncompatibleAssignment = "E003";
    public const string IncompatibleOperation = "E004";
    public const string NonBooleanCondition = "E005";
    public const string InvalidReturn = "E006";
    public const string WrongArgumentCount = "E007";
    public const string IncompatibleArguments = "E008";
    public const string ConstantWrite = "E009";
    public const string InvalidSilenceUsage = "E010";
    public const string SessionMissingOrDuplicated = "E011";
    public const string ImportCycle = "E012";
    public const string FunctionOverload = "E013";
    public const string ImportNotFound = "E014";
    public const string ModuleWithSession = "E015";
    public const string MissingProjectFile = "E016";
    public const string MultipleProjectFiles = "E017";
    public const string NonConstantConstantInitializer = "E018";
    public const string RaggedMatrix = "E019";
    public const string UnimportedFunction = "E020";

    // ---- Errores léxicos (7.2.2) ----
    public const string InvalidCharacter = "E021";
    public const string UnterminatedString = "E022";
    public const string MalformedCharacterLiteral = "E023";
    public const string InvalidEscapeSequence = "E024";
    public const string UnterminatedBlockComment = "E025";
    public const string MalformedNumericLiteral = "E026";

    // ---- Errores sintácticos (7.2.3) ----
    public const string MissingSemicolon = "E027";
    public const string MissingBlockDelimiter = "E028";
    public const string MissingConditionBrackets = "E029";
    public const string MalformedFunctionDeclaration = "E030";
    public const string MalformedParameterList = "E031";
    public const string ExpectedExpression = "E032";
    public const string UnclosedGrouping = "E033";
    public const string MalformedVariableDeclaration = "E034";
    public const string MalformedInputOutput = "E035";
    public const string MalformedLoopHeader = "E036";
    public const string MalformedMixStatement = "E037";
    public const string InvalidStatement = "E038";
    public const string UnexpectedTopLevelElement = "E039";

    // ---- Advertencias (§7.4) ----
    public const string UnusedVariable = "W001";
    public const string ShadowedIdentifier = "W002";
    public const string UnusedFunction = "W003";
    public const string ImplicitPhraseConversion = "W004";
}
