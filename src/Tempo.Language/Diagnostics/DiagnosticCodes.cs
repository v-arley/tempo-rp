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

    // Los errores léxicos y sintácticos se emiten con un código genérico hasta
    // que el catálogo E021-E039 (§7.2.2 y §7.2.3) esté asignado por tipo.
    public const string LexicalOrSyntaxError = "E000";

    // ---- Advertencias (§7.4) ----
    public const string UnusedVariable = "W001";
    public const string ShadowedIdentifier = "W002";
    public const string UnusedFunction = "W003";
    public const string ImplicitPhraseConversion = "W004";
}
