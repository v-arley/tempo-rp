namespace Tempo.Language.Diagnostics;

/// <summary>
/// Títulos oficiales de cada código de diagnóstico, transcritos de las
/// tablas §7.2 y §7.4 de la especificación TM v1.1. Fuente única: el
/// analizador nunca redacta un título por su cuenta.
/// </summary>

public static class DiagnosticCatalog
{
    private static readonly Dictionary<string, string> Titles =
        new(StringComparer.Ordinal)
        {
            // §7.2 — Errores de compilación
            ["E001"] = "Identificador no declarado",
            ["E002"] = "Redeclaracion de identificador en el mismo ambito",
            ["E003"] = "Tipo incompatible en asignacion",
            ["E004"] = "Tipo incompatible en operacion",
            ["E005"] = "Condicion no booleana en if/repeat/loop",
            ["E006"] = "Funcion void retorna valor / funcion no-void no retorna valor",
            ["E007"] = "Numero incorrecto de argumentos en llamada",
            ["E008"] = "Tipos de argumentos incompatibles en llamada",
            ["E009"] = "Acceso a variable const en escritura",
            ["E010"] = "Uso de silence en tipo no permitido",
            ["E011"] = "Session ausente o multiple",
            ["E012"] = "Importacion ciclica detectada",
            ["E013"] = "Sobrecarga de funcion no permitida",
            ["E014"] = "Archivo importado no encontrado",
            ["E015"] = "Archivo .tm contiene session",
            ["E016"] = "Proyecto sin archivo .tmpr",
            ["E017"] = "Proyecto con multiples archivos .tmpr",
            ["E018"] = "Constante inicializada con expresion no constante",
            ["E019"] = "Matriz con filas de distinto tamano",
            ["E020"] = "Forward reference a funcion no resuelta",

            // 7.2.2 — Errores léxicos
            [DiagnosticCodes.InvalidCharacter] = "Caracter no valido",
            [DiagnosticCodes.UnterminatedString] = "Literal de cadena no cerrado",
            [DiagnosticCodes.MalformedCharacterLiteral] = "Literal de caracter mal formado",
            [DiagnosticCodes.InvalidEscapeSequence] = "Secuencia de escape no valida",
            [DiagnosticCodes.UnterminatedBlockComment] = "Comentario de bloque no cerrado",
            [DiagnosticCodes.MalformedNumericLiteral] = "Literal numerico mal formado",

            // 7.2.3 — Errores sintácticos
            [DiagnosticCodes.MissingSemicolon] = "Se esperaba `;` al final de la sentencia",
            [DiagnosticCodes.MissingBlockDelimiter] = "Delimitador de bloque esperado",
            [DiagnosticCodes.MissingConditionBrackets] = "Condicion sin corchetes",
            [DiagnosticCodes.MalformedFunctionDeclaration] = "Declaracion de funcion mal formada",
            [DiagnosticCodes.MalformedParameterList] = "Lista de parametros mal formada",
            [DiagnosticCodes.ExpectedExpression] = "Se esperaba una expresion",
            [DiagnosticCodes.UnclosedGrouping] = "Agrupacion sin cerrar",
            [DiagnosticCodes.MalformedVariableDeclaration] = "Declaracion de variable o constante mal formada",
            [DiagnosticCodes.MalformedInputOutput] = "Sentencia de entrada/salida mal formada",
            [DiagnosticCodes.MalformedLoopHeader] = "Cabecera de `loop` mal formada",
            [DiagnosticCodes.MalformedMixStatement] = "Sentencia `mix` mal formada",
            [DiagnosticCodes.InvalidStatement] = "Sentencia no valida",
            [DiagnosticCodes.UnexpectedTopLevelElement] = "Elemento inesperado en el nivel superior",

            // §7.4 — Advertencias
            ["W001"] = "Variable declarada pero no utilizada",
            ["W002"] = "Sombreado de identificador (shadowing)",
            ["W003"] = "Funcion declarada pero nunca invocada",
            ["W004"] = "Conversion implicita en concatenacion phrase"
        };

    /// <summary>
    /// Devuelve el título oficial del código indicado, o una etiqueta
    /// genérica si el código no pertenece al catálogo.
    /// </summary>
    public static string TitleOf(string code)
    {
        if (code is not null && Titles.TryGetValue(code, out string title))
        {
            return title;
        }

        return "Diagnostico sin catalogar";
    }
}
