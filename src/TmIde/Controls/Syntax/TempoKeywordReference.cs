
using System;
using System.Collections.Generic;

namespace TmIde.Controls.Syntax;

/// Información educativa de una palabra reservada
public sealed record TempoKeywordInfo(
    string Keyword,
    string Equivalent,
    string Description);

/// Catálogo de equivalencias y descripciones de las palabras reservadas de Tempo
public static class TempoKeywordReference
{
    private static readonly Dictionary<string, TempoKeywordInfo>
        Keywords = new(StringComparer.Ordinal)
    {
        // Tipos de datos
        ["sample"] = new(
            "sample",
            "int",
            "Tipo de dato que almacena números enteros con signo de 32 bits."),

        ["wave"] = new(
            "wave",
            "float",
            "Tipo de dato que almacena números decimales de precisión simple."),

        ["tone"] = new(
            "tone",
            "char (conceptual)",
            "Tipo de dato que representa un carácter Unicode."),

        ["phrase"] = new(
            "phrase",
            "string",
            "Tipo de dato que almacena cadenas de texto."),

        ["signal"] = new(
            "signal",
            "bool",
            "Tipo de dato lógico que puede contener true o false."),

        ["silence"] = new(
            "silence",
            "null",
            "Representa la ausencia de un valor."),

        ["void"] = new(
            "void",
            "void",
            "Indica que una función no devuelve ningún valor."),

        // Tipos compuestos
        ["sequence"] = new(
            "sequence",
            "arreglo / array",
            "Define un arreglo unidimensional de elementos del mismo tipo."),

        ["score"] = new(
            "score",
            "matriz bidimensional",
            "Define una matriz de dos dimensiones con elementos del mismo tipo."),

        // Estructura del programa
        ["session"] = new(
            "session",
            "main() / Main()",
            "Define el punto de entrada donde comienza la ejecución del programa."),

        ["compose"] = new(
            "compose",
            "declaración de función",
            "Permite definir una función con nombre, parámetros y tipo de retorno."),

        ["ret"] = new(
            "ret",
            "return",
            "Finaliza una función y permite devolver un valor al punto de llamada."),

        ["import"] = new(
            "import",
            "import / inclusión de módulo",
            "Permite utilizar funciones definidas en otro archivo .tm."),

        ["const"] = new(
            "const",
            "const",
            "Declara una variable cuyo valor no puede modificarse después de inicializarse."),

        // Entrada y salida
        ["output"] = new(
            "output",
            "Console.WriteLine() / cout",
            "Muestra el resultado de una expresión en la salida del programa."),

        ["input"] = new(
            "input",
            "Console.ReadLine() / cin",
            "Lee un valor proporcionado por el usuario y lo almacena en una variable."),

        // Estructuras de control
        ["if"] = new(
            "if",
            "if",
            "Ejecuta un bloque cuando una condición es verdadera."),

        ["else"] = new(
            "else",
            "else",
            "Ejecuta una alternativa cuando la condición anterior no se cumple."),

        ["mix"] = new(
            "mix",
            "switch",
            "Selecciona un bloque entre distintas alternativas según un valor."),

        ["case"] = new(
            "case",
            "case",
            "Define una alternativa específica dentro de una estructura mix."),

        ["default"] = new(
            "default",
            "default",
            "Define la alternativa que se ejecuta cuando ningún case coincide."),

        ["loop"] = new(
            "loop",
            "for",
            "Repite instrucciones usando inicialización, condición y actualización."),

        ["repeat"] = new(
            "repeat",
            "while",
            "Repite un bloque mientras su condición sea verdadera."),

        // Operadores lógicos
        ["and"] = new(
            "and",
            "&&",
            "Operador lógico AND: devuelve verdadero cuando ambas condiciones lo son."),

        ["or"] = new(
            "or",
            "||",
            "Operador lógico OR: devuelve verdadero cuando al menos una condición lo es."),

        ["not"] = new(
            "not",
            "!",
            "Operador lógico NOT: invierte el valor de una condición."),

        // Valores lógicos
        ["true"] = new(
            "true",
            "true",
            "Literal lógico que representa el valor verdadero."),

        ["false"] = new(
            "false",
            "false",
            "Literal lógico que representa el valor falso."),

        // Funcionalidades propias de Tempo
        ["tempo"] = new(
            "tempo",
            "configuración BPM",
            "Establece el ritmo del visualizador de partículas del proyecto, entre 20 y 300 BPM."),

        ["pulse"] = new(
            "pulse",
            "emisión de evento visual",
            "Genera un pulso de partículas en el visualizador durante la ejecución.")
    };

    /// <summary>
    /// Busca información de una palabra reservada.
    /// La búsqueda distingue mayúsculas y minúsculas.
    /// </summary>
    public static TempoKeywordInfo? Find(string keyword)
    {
        return Keywords.TryGetValue(keyword, out var info)
            ? info
            : null;
    }

    /// <summary>
    /// Cantidad de entradas disponibles.
    /// </summary>
    public static int Count => Keywords.Count;
}
