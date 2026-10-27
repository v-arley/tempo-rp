
using System.Collections.Generic;

using CommunityToolkit.Mvvm.ComponentModel;

using TmIde.ViewModels;

namespace TmIde.ViewModels.Help;

public sealed record LanguageReferenceSection(
    string Title,
    string Summary,
    string Details,
    string Example,
    string Note);

public sealed partial class LanguageReferenceViewModel : ViewModelBase
{
    [ObservableProperty]
    private LanguageReferenceSection? _selectedSection;

    public IReadOnlyList<LanguageReferenceSection> Sections { get; }

    public LanguageReferenceViewModel()
    {
        Sections = CreateSections();
        SelectedSection = Sections[0];
    }

    private static IReadOnlyList<LanguageReferenceSection> CreateSections()
    {
        return
        [
            new(
                "Palabras reservadas",
                "Conoce las palabras que tienen un significado especial en Tempo.",
                """
                Las palabras reservadas forman parte del lenguaje y no pueden utilizarse como nombres de variables o funciones.

                Declaraciones y tipos:
                sample, wave, tone, phrase, signal, silence, sequence, score, void, const

                Estructura y funciones:
                session, compose, ret, import

                Control de flujo:
                if, else, mix, case, default, loop, repeat

                Entrada y salida:
                input, output

                Valores y características especiales:
                true, false, tempo, pulse

                Tempo distingue entre mayúsculas y minúsculas.
                """,
                """
                session >
                    sample edad = 20;
                    phrase nombre = "Tempo";
                    signal activo = true;

                    output >> edad;
                <
                """,
                "sample es un tipo de dato entero. session representa el punto de entrada del programa."
            ),

            new(
                "Sintaxis",
                "Aprende cómo se escriben los programas y las instrucciones.",
                """
                Los archivos principales utilizan la extensión .tmpr.
                Los módulos utilizan la extensión .tm.

                Los bloques se abren con > y se cierran con <.

                Las instrucciones, como declaraciones y asignaciones, terminan con punto y coma (;).

                Los comentarios de una línea comienzan con #.
                Los comentarios de bloque se escriben entre ## y ##.

                La indentación facilita la lectura, pero no determina los bloques.

                El archivo principal debe contener una sentencia session.
                """,
                """
                # Mi primer programa

                session >
                    sample numero = 10;
                    sample resultado = numero + 5;

                    output >> resultado;
                <
                """,
                "Solo debe existir un archivo principal .tmpr por proyecto."
            ),

            new(
                "Estructuras de control",
                "Toma decisiones y repite instrucciones con condiciones.",
                """
                if:
                Ejecuta instrucciones cuando la condición es verdadera.

                else:
                Define una alternativa cuando la condición anterior no se cumple.

                repeat:
                Repite un bloque mientras su condición sea verdadera.

                loop:
                Utiliza inicialización, condición y actualización.

                mix:
                Selecciona una alternativa mediante case y default.

                Las condiciones de if, repeat y loop deben producir un valor signal.
                """,
                """
                session >
                    sample contador = 0;

                    repeat [contador < 3] >
                        output >> contador;
                        contador++;
                    <

                    if [contador == 3] >
                        output >> "Finalizado";
                    <
                    else >
                        output >> "Incompleto";
                    <
                <
                """,
                "Los delimitadores > y < definen los bloques de cada estructura."
            ),

            new(
                "Funciones",
                "Organiza el programa en operaciones reutilizables.",
                """
                Las funciones se declaran con la palabra compose.

                Su estructura general es:

                tipo : compose nombre(parámetros) > ... <

                El tipo aparece antes de los dos puntos.

                Una función puede recibir parámetros y retornar un valor con ret.

                void representa una función sin valor de retorno.

                Tempo permite la recursión y no permite sobrecargar funciones con el mismo nombre.
                """,
                """
                sample : compose sumar(sample a, sample b) >
                    ret a + b;
                <

                session >
                    sample resultado = sumar(5, 3);
                    output >> resultado;
                <
                """,
                "Las funciones se declaran fuera de session y pueden invocarse desde ella."
            ),

            new(
                "Operaciones",
                "Utiliza operadores aritméticos, de comparación y lógicos.",
                """
                Operadores aritméticos:
                +   Suma
                -   Resta
                *   Multiplicación
                /   División

                Comparaciones:
                ==  Igualdad
                !=  Diferencia
                >   Mayor que
                <   Menor que
                >=  Mayor o igual
                <=  Menor o igual

                Operadores lógicos:
                and, or, not

                Actualizaciones:
                ++  Incremento
                --  Decremento

                El operador + también permite concatenar valores phrase.
                La división entre dos sample produce un resultado entero.
                """,
                """
                session >
                    sample a = 10;
                    sample b = 3;

                    output >> a + b;
                    output >> a * b;
                    output >> a / b;

                    signal mayor = a > b;
                    output >> mayor;
                <
                """,
                "La división entre cero produce un error durante la ejecución."
            ),

            new(
                "Semántica",
                "Comprende las reglas que deben cumplir los programas.",
                """
                Tempo utiliza ámbitos léxicos.

                Cada bloque define un ámbito para sus variables.

                Las variables deben inicializarse cuando se declaran.

                No se permite declarar dos variables con el mismo nombre en el mismo ámbito.

                const define variables cuyo valor no puede modificarse después de su declaración.

                Las expresiones y los valores asignados deben respetar los tipos de datos.

                Las funciones deben respetar los tipos de sus parámetros y valores de retorno.

                Las variables pertenecen a los bloques; entre archivos se comparten funciones mediante importaciones.
                """,
                """
                session >
                    const sample MAXIMO = 100;
                    sample numero = 25;

                    if [numero < MAXIMO] >
                        phrase mensaje = "Valor valido";
                        output >> mensaje;
                    <
                <
                """,
                "El analizador semántico detecta problemas de tipos, declaraciones y ámbitos antes de ejecutar."
            ),

            new(
                "Tipos de datos",
                "Identifica los tipos primitivos y compuestos del lenguaje.",
                """
                Tipos primitivos:

                sample: entero de 32 bits.
                wave: número decimal de precisión simple.
                tone: carácter.
                phrase: cadena de texto.
                signal: valor lógico true o false.
                silence: valor nulo.

                Tipos compuestos:

                sequence<T>: arreglo unidimensional de elementos del mismo tipo.

                score<T>: matriz bidimensional de elementos del mismo tipo.

                void se utiliza como tipo de retorno para funciones sin resultado.

                Los índices de sequence y score comienzan en cero.
                """,
                """
                session >
                    sample edad = 20;
                    wave precio = 3.50;
                    tone letra = 'A';
                    phrase saludo = "Hola";
                    signal activo = true;

                    sequence<sample> numeros = [1, 2, 3];

                    output >> numeros[0];
                <
                """,
                "sequence y score tienen tamaño fijo y permiten consultar length()."
            ),

            new(
                "Importaciones",
                "Reutiliza funciones definidas en otros archivos Tempo.",
                """
                Una importación permite utilizar funciones de un módulo .tm desde otro archivo.

                Se escribe:

                import "archivo.tm";

                La importación debe aparecer a nivel superior, fuera de session.

                Los módulos pueden contener funciones e importaciones, pero no pueden contener session.

                El archivo .tmpr representa el punto de entrada del programa.

                Tempo comprueba que los archivos importados existan, y detecta problemas como dependencias circulares y funciones duplicadas.
                """,
                """
                # Archivo: principal.tmpr

                import "operaciones.tm";

                session >
                    sample resultado = doble(5);
                    output >> resultado;
                <
                """,
                "El módulo operaciones.tm debe declarar la función doble(sample x) para que este ejemplo compile."
            ),

            new(
                "Ejemplos",
                "Practica con un programa completo que combina varias características.",
                """
                Este programa utiliza:

                - Una función que recibe un parámetro.
                - Una variable entera.
                - Una estructura condicional.
                - Una llamada a función.
                - La instrucción output.

                Puedes copiarlo y pegarlo en el archivo principal de un proyecto de prueba.
                """,
                """
                sample : compose cuadrado(sample n) >
                    ret n * n;
                <

                session >
                    sample numero = 5;
                    sample resultado = cuadrado(numero);

                    if [resultado > 20] >
                        output >> "Resultado mayor a 20";
                    <
                    else >
                        output >> "Resultado menor o igual a 20";
                    <

                    output >> resultado;
                <
                """,
                "Este programa debe mostrar el mensaje de resultado mayor a 20 y luego el número 25."
            )
        ];
    }
}
