# Especificación del lenguaje Tempo (TM)

Este documento define el lenguaje de programación Tempo, abreviado **TM**: su
estructura léxica, su gramática, su sistema de tipos, sus reglas semánticas, la
organización de los programas en proyectos, el catálogo de diagnósticos y la
semántica de ejecución. La implementación de referencia es la biblioteca
`Tempo.Language` de este repositorio.

## Contenido

1. [Introducción](#1-introducción)
2. [Estructura léxica](#2-estructura-léxica)
3. [Gramática](#3-gramática)
4. [Sistema de tipos](#4-sistema-de-tipos)
5. [Semántica estática](#5-semántica-estática)
6. [Programas y proyectos](#6-programas-y-proyectos)
7. [Diagnósticos](#7-diagnósticos)
8. [Programas de ejemplo](#8-programas-de-ejemplo)
9. [Semántica de ejecución](#9-semántica-de-ejecución)
10. [Casos de verificación](#10-casos-de-verificación)
11. [Interfaz de ejecución](#11-interfaz-de-ejecución)
12. [Implementación de referencia](#12-implementación-de-referencia)

---

## 1. Introducción

TM es un lenguaje imperativo, estructurado y de tipado estático fuerte. Su
vocabulario toma términos de la música: una función se *compone*, el punto de
entrada es la *sesión* y los tipos de datos se llaman *sample*, *wave*, *tone* o
*phrase*.

Características generales:

| Aspecto | Definición |
|---|---|
| Paradigma | Imperativo y estructurado, con funciones de primer orden no anidadas. |
| Tipado | Estático y fuerte; sin conversiones implícitas entre tipos numéricos. |
| Bloques | Delimitados por `>` (apertura) y `<` (cierre). |
| Condiciones | Encerradas entre corchetes: `if [x > 0]`. |
| Sentencias | Terminadas en `;`. |
| Unidad de compilación | El proyecto: un archivo principal `.tmpr` y módulos `.tm`. |
| Modelo de ejecución | Interpretado sobre el árbol de sintaxis abstracta. |

Programa mínimo:

```tm
session >
    output >> "Hola, Mundo";
<
```

### 1.1 Notación

La gramática se expresa en EBNF (ISO/IEC 14977): `=` define una regla, `,`
concatena, `|` separa alternativas, `[ ... ]` indica una parte opcional,
`{ ... }` una repetición de cero o más veces y `;` cierra la regla. Los
terminales se escriben entre comillas dobles.

---

## 2. Estructura léxica

El análisis léxico convierte el texto fuente en una secuencia de *tokens*. Lo
hace de izquierda a derecha, eligiendo en cada posición el token más largo
posible, y termina siempre con un token de fin de archivo.

### 2.1 Juego de caracteres

Los archivos fuente se codifican en UTF-8. Fuera de los literales de cadena, de
carácter y de los comentarios, solo se admiten letras ASCII, dígitos, el guion
bajo, los símbolos de la sección 2.6 y el espacio en blanco. Cualquier otro
carácter produce el error léxico **E021**.

### 2.2 Espacio en blanco y comentarios

El espacio, la tabulación, el retorno de carro y el salto de línea separan
tokens y no tienen otro significado. Los comentarios se descartan:

| Forma | Sintaxis | Alcance |
|---|---|---|
| Comentario de línea | `# texto` | Desde `#` hasta el final de la línea. |
| Comentario de bloque | `## texto ##` | Desde `##` hasta el siguiente `##`; puede abarcar varias líneas. |

Los comentarios de bloque no se anidan: el primer `##` posterior a la apertura
cierra el comentario. Un comentario de bloque sin cierre produce **E025**.

```tm
# Comentario de una línea
## Comentario
   de varias líneas ##
```

### 2.3 Identificadores

```ebnf
identifier = letter, { letter | digit | "_" } ;
letter     = "a" .. "z" | "A" .. "Z" ;
digit      = "0" .. "9" ;
```

Un identificador empieza por una letra ASCII y continúa con letras, dígitos o
guiones bajos. Los identificadores distinguen mayúsculas de minúsculas: `total`
y `Total` son nombres distintos. Una palabra reservada no puede usarse como
identificador.

### 2.4 Palabras reservadas

Las palabras reservadas se escriben siempre en minúsculas.

| Categoría | Palabras |
|---|---|
| Estructura y control | `compose` `session` `output` `input` `mix` `case` `default` `loop` `repeat` `if` `else` `ret` `import` `const` `tempo` `pulse` |
| Operadores lógicos | `and` `or` `not` |
| Tipos primitivos | `sample` `wave` `tone` `phrase` `signal` |
| Tipo de retorno vacío | `void` |
| Tipos compuestos | `sequence` `score` |
| Valores predefinidos | `true` `false` `silence` |

Equivalencia de cada palabra con su concepto general:

| TM | Concepto | TM | Concepto |
|---|---|---|---|
| `sample` | entero | `session` | punto de entrada (`main`) |
| `wave` | real | `compose` | declaración de función |
| `tone` | carácter | `ret` | retorno |
| `phrase` | cadena | `output` | escritura en la salida |
| `signal` | booleano | `input` | lectura de la entrada |
| `silence` | valor nulo | `mix` / `case` / `default` | selección múltiple (`switch`) |
| `void` | sin valor de retorno | `loop` | bucle con contador (`for`) |
| `sequence<T>` | arreglo unidimensional | `repeat` | bucle condicional (`while`) |
| `score<T>` | matriz bidimensional | `tempo` | ritmo del proyecto, en BPM |
| `const` | constante | `pulse` | ráfaga visual de intensidad variable |

### 2.5 Literales

```ebnf
integer_literal = digit, { digit } ;
float_literal   = digit, { digit }, ".", digit, { digit } ;
string_literal  = '"', { string_char | escape_sequence }, '"' ;
char_literal    = "'", ( char_char | escape_sequence ), "'" ;
bool_literal    = "true" | "false" ;
null_literal    = "silence" ;
```

| Literal | Tipo | Ejemplos | Reglas |
|---|---|---|---|
| Entero | `sample` | `0`, `42`, `1000` | Solo dígitos decimales. |
| Real | `wave` | `3.14`, `0.5` | Exige al menos un dígito a cada lado del punto: `5.` y `.5` no son válidos (**E026**). |
| Cadena | `phrase` | `"Hola"`, `""` | No puede contener un salto de línea ni quedar sin cerrar (**E022**). |
| Carácter | `tone` | `'a'`, `'\n'` | Exactamente un carácter o una secuencia de escape (**E023**). |
| Booleano | `signal` | `true`, `false` | |
| Nulo | `silence` | `silence` | |

Los literales numéricos no llevan signo: en `-5` el signo es el operador unario
`-` aplicado al literal `5`.

Secuencias de escape admitidas (cualquier otra produce **E024**):

| Secuencia | Significado | En cadenas | En caracteres |
|---|---|---|---|
| `\n` | salto de línea | sí | sí |
| `\t` | tabulación | sí | sí |
| `\\` | barra invertida | sí | sí |
| `\"` | comilla doble | sí | no |
| `\'` | comilla simple | no | sí |

### 2.6 Operadores y signos de puntuación

| Categoría | Símbolos |
|---|---|
| Aritméticos | `+` `-` `*` `/` |
| Actualización | `++` `--` |
| Relacionales | `==` `!=` `>` `<` `>=` `<=` |
| Asignación | `=` |
| Flujo de E/S | `>>` (salida), `<<` (entrada) |
| Agrupación | `(` `)` `[` `]` |
| Puntuación | `:` `;` `,` `.` |
| Reservados sin uso | `{` `}` |

Los operadores de dos caracteres se reconocen antes que los de uno: `>>` es
siempre el operador de salida y `>=` el relacional, nunca dos tokens. El
carácter `!` solo es válido como parte de `!=`.

### 2.7 Desambiguación de `>` y `<`

Los símbolos `>` y `<` cumplen tres papeles: delimitadores de bloque,
operadores relacionales y corchetes de los tipos compuestos. El analizador
léxico los emite siempre como el mismo token y es el analizador sintáctico
quien decide su papel según la posición:

1. **Tipo compuesto.** Inmediatamente después de `sequence` o `score`, `<` y
   `>` encierran el tipo de los elementos: `sequence<sample>`.
2. **Apertura de bloque.** Donde la gramática espera un bloque (después de
   `session`, de la firma de una función, de una condición, de `else` o de
   `case ...:`), `>` abre el bloque.
3. **Selector de `mix`.** En la expresión de control de `mix`, un `>` seguido
   de `case`, `default` o `<` abre el cuerpo del `mix` y no es una comparación.
4. **Cierre de bloque.** Dentro de una expresión, un `<` se interpreta como
   cierre de bloque, y no como comparación, cuando le sigue el fin del archivo,
   otro `<` o `else`, o cuando ocupa una línea propia (el token anterior está en
   una línea previa y el siguiente en una línea posterior).
5. En cualquier otra posición son operadores relacionales.

Como consecuencia práctica, el `<` que cierra un bloque se escribe en su propia
línea o seguido de `else`:

```tm
if [x > 0] >
    output >> "positivo";
< else >
    output >> "no positivo";
<
```

---

## 3. Gramática

### 3.1 Estructura de un archivo

```ebnf
program        = { import_decl }, { tempo_decl }, { function_decl },
                 [ session_decl ] ;

import_decl    = "import", string_literal, ";" ;
tempo_decl     = "tempo", integer_literal, ";" ;
session_decl   = "session", block ;

function_decl  = return_type, ":", "compose", identifier,
                 "(", [ parameter_list ], ")", block ;
return_type    = type | "void" ;
parameter_list = parameter, { ",", parameter } ;
parameter      = type, identifier ;

block          = ">", { statement }, "<" ;
```

Las secciones del archivo aparecen en este orden fijo: importaciones,
declaración `tempo`, funciones y, por último, `session`. Después de `session`
solo puede seguir el fin del archivo. Las restricciones sobre cuántas
declaraciones `tempo` y `session` admite cada archivo son semánticas
(secciones 6.3 y 6.4).

### 3.2 Tipos

```ebnf
type           = primitive_type | composite_type ;
primitive_type = "sample" | "wave" | "tone" | "phrase" | "signal" ;
composite_type = ( "sequence" | "score" ), "<", type, ">" ;
```

### 3.3 Sentencias

```ebnf
statement      = var_decl | const_decl
               | assign_stmt | indexed_assign | update_stmt | call_stmt
               | output_stmt | input_stmt | ret_stmt
               | if_stmt | repeat_stmt | loop_stmt | mix_stmt
               | pulse_stmt ;

var_decl       = type, identifier, "=", expression, ";" ;
const_decl     = "const", type, identifier, "=", expression, ";" ;

assign_stmt    = identifier, "=", expression, ";" ;
indexed_assign = identifier, "[", expression, "]", { "[", expression, "]" },
                 "=", expression, ";" ;
update_stmt    = identifier, ( "++" | "--" ), ";" ;
call_stmt      = call_expr, ";" ;

output_stmt    = "output", ">>", expression, ";" ;
input_stmt     = "input", "<<", identifier, ";" ;
ret_stmt       = "ret", [ expression ], ";" ;

if_stmt        = "if", condition, block,
                 { "else", "if", condition, block },
                 [ "else", block ] ;
repeat_stmt    = "repeat", condition, block ;
loop_stmt      = "loop", "[", var_decl, expression, ";", loop_update, "]",
                 block ;
loop_update    = identifier, ( "++" | "--" )
               | identifier, "=", expression ;
condition      = "[", expression, "]" ;

mix_stmt       = "mix", expression, ">", { case_clause | default_clause }, "<" ;
case_clause    = "case", expression, ":", block ;
default_clause = "default", ":", block ;

pulse_stmt     = "pulse", "[", expression, "]", ";" ;
```

Restricciones sintácticas adicionales:

- Toda variable y toda constante se declaran con un valor inicial.
- La única expresión que puede usarse como sentencia es la llamada a función.
- Un `mix` admite como máximo una cláusula `default` (**E037**).
- El primer componente de la cabecera de `loop` es siempre una declaración de
  variable (**E036**).

### 3.4 Expresiones

```ebnf
expression          = or_expr ;
or_expr             = and_expr, { "or", and_expr } ;
and_expr            = equality_expr, { "and", equality_expr } ;
equality_expr       = relational_expr, { ( "==" | "!=" ), relational_expr } ;
relational_expr     = additive_expr, { ( ">" | "<" | ">=" | "<=" ), additive_expr } ;
additive_expr       = multiplicative_expr, { ( "+" | "-" ), multiplicative_expr } ;
multiplicative_expr = unary_expr, { ( "*" | "/" ), unary_expr } ;
unary_expr          = ( "not" | "-" ), unary_expr | postfix_expr ;
postfix_expr        = primary_expr, { "[", expression, "]" | ".", "length", "(", ")" } ;
primary_expr        = literal | identifier | call_expr
                    | "(", expression, ")" | collection_literal ;

call_expr           = identifier, "(", [ expression, { ",", expression } ], ")" ;
collection_literal  = "[", [ expression, { ",", expression } ], "]" ;
literal             = integer_literal | float_literal | string_literal
                    | char_literal | "true" | "false" | "silence" ;
```

El único miembro accesible con `.` es `length()`; cualquier otro nombre produce
**E032**.

---

## 4. Sistema de tipos

### 4.1 Tipos primitivos

| Tipo | Valores | Representación |
|---|---|---|
| `sample` | Enteros con signo | 32 bits, de −2 147 483 648 a 2 147 483 647. |
| `wave` | Números reales | Coma flotante IEEE 754 de precisión simple (32 bits). |
| `tone` | Un carácter | Una unidad de código UTF-16. |
| `phrase` | Cadenas de texto | Secuencia inmutable de caracteres. |
| `signal` | `true`, `false` | Booleano. |

Además existen dos tipos especiales que no son primitivos:

- **`silence`** es el tipo del valor nulo `silence`. No es declarable: no puede
  usarse como tipo de una variable, constante o parámetro (**E010**).
- **`void`** solo puede aparecer como tipo de retorno de una función. Declarar
  una variable o un parámetro `void` produce **E004**.

### 4.2 Tipos compuestos

| Tipo | Descripción |
|---|---|
| `sequence<T>` | Colección unidimensional y homogénea de elementos de tipo `T`. |
| `score<T>` | Matriz bidimensional rectangular de elementos de tipo `T`. |

Las colecciones se construyen con literales de colección. Un `score` se escribe
como una lista de filas, y todas las filas deben tener el mismo número de
columnas (**E019**):

```tm
sequence<sample> notas = [60, 62, 64];
score<wave> matriz = [[1.0, 2.0], [3.0, 4.0]];
```

Un literal de colección toma su tipo de elemento del contexto (la variable, el
parámetro o el retorno al que se asigna). Sin contexto se infiere del primer
elemento; una colección vacía sin contexto es un error (**E004**). Todos los
elementos deben ser del tipo de elemento (**E003**).

Las colecciones tienen tamaño fijo y elementos mutables. Su valor es una
referencia: asignar una colección a otra variable o pasarla como argumento
comparte los mismos elementos.

### 4.3 Variables y constantes

Una variable se declara con su tipo, su nombre y su valor inicial; puede
modificarse después por asignación, por indexación, con `++`/`--` o con
`input`. Una constante se declara anteponiendo `const` y es inmutable:
cualquier escritura posterior produce **E009**.

```tm
sample contador = 0;
const sample LIMITE = 100;
```

Los parámetros de una función se comportan como variables locales mutables.

### 4.4 Reglas de tipo

#### 4.4.1 Asignabilidad

Un valor de tipo `S` puede asignarse a un destino de tipo `T` (en una
declaración, una asignación, un argumento, un retorno o un elemento de
colección) solo si `S` y `T` son el mismo tipo. Los tipos compuestos se
comparan estructuralmente: `sequence<sample>` es asignable a
`sequence<sample>` pero no a `sequence<wave>`. Una asignación incompatible
produce **E003**.

##### 4.4.1.1 Ausencia de coerción

TM no realiza conversiones implícitas entre tipos. En particular, `sample` y
`wave` son incompatibles en ambos sentidos: `wave w = 1;` y `sample s = 3.14;`
son errores **E003**. Para operar con reales se escriben literales reales:
`wave w = 1.0;`.

##### 4.4.1.2 Concatenación con `phrase`

El operador `+` con un operando `phrase` y el otro de cualquier tipo primitivo,
en cualquier orden, produce un `phrase` que concatena la representación textual
de ambos. Cuando el otro operando no es un `phrase`, la conversión implícita a
texto se advierte con **W004**.

| Tipo | Representación textual |
|---|---|
| `sample` | Número decimal: `42`, `-7`. |
| `wave` | Hasta nueve cifras significativas, con `.0` si el valor es entero: `3.0`, `0.5`. |
| `tone` | El propio carácter. |
| `signal` | `true` o `false`. |

##### 4.4.1.3 El valor `silence`

`silence` es asignable únicamente a destinos de tipo `phrase`. Asignarlo a
cualquier otro tipo produce **E010**. Puede compararse con `==` y `!=` contra
un `phrase` o contra otro `silence`, y puede escribirse con `output`.

#### 4.4.2 Tabla de operadores

Toda combinación de operador y tipos que no figure en la tabla es un error
**E004**.

| Operador | Operandos | Resultado |
|---|---|---|
| `+` `-` `*` `/` | `sample`, `sample` | `sample` |
| `+` `-` `*` `/` | `wave`, `wave` | `wave` |
| `+` | `phrase`, `phrase` | `phrase` |
| `+` | `phrase` y otro primitivo (en cualquier orden) | `phrase` (con **W004**) |
| `==` `!=` | mismo tipo primitivo en ambos lados | `signal` |
| `==` `!=` | `phrase` o `silence` frente a `silence` | `signal` |
| `>` `<` `>=` `<=` | `sample`, `sample` | `signal` |
| `>` `<` `>=` `<=` | `wave`, `wave` | `signal` |
| `and` `or` | `signal`, `signal` | `signal` |
| `not` (unario) | `signal` | `signal` |
| `-` (unario) | `sample` | `sample` |
| `-` (unario) | `wave` | `wave` |

#### 4.4.3 Precedencia y asociatividad

De mayor a menor precedencia. Todos los operadores binarios asocian por la
izquierda; los paréntesis alteran el orden de evaluación.

| Nivel | Operadores | Descripción |
|---|---|---|
| 1 | `x[i]`, `x.length()` | Indexación y longitud (postfijos) |
| 2 | `not`, `-` | Unarios |
| 3 | `*`, `/` | Multiplicativos |
| 4 | `+`, `-` | Aditivos |
| 5 | `>`, `<`, `>=`, `<=` | Relacionales |
| 6 | `==`, `!=` | Igualdad |
| 7 | `and` | Conjunción |
| 8 | `or` | Disyunción |

#### 4.4.4 Indexación y longitud

- Un `sequence<T>` se indexa con un índice: `s[i]` es de tipo `T`.
- Un `score<T>` se indexa con dos índices: `m[fila][columna]` es de tipo `T`.
- Todo índice es de tipo `sample` (**E004**).
- Indexar un valor que no es colección, o usar un número de índices distinto
  del que exige su tipo, produce **E004**.
- `x.length()` es de tipo `sample` y solo está definido sobre colecciones: en un
  `sequence` devuelve el número de elementos y en un `score`, el de filas.

#### 4.4.5 Colecciones y operadores

Ningún operador admite operandos de tipo `sequence` o `score`, ni siquiera la
igualdad. Las colecciones se recorren y se comparan elemento a elemento.

---

## 5. Semántica estática

El análisis semántico verifica cada archivo después de construir su árbol
sintáctico. Acumula todos los diagnósticos que encuentra en lugar de detenerse
en el primero, y un error de origen no se repite en sus usos posteriores: un
identificador no declarado se informa una sola vez y los errores que dependen
de un tipo ya erróneo no se informan.

### 5.1 Ámbitos

Un nombre debe declararse antes de usarse (**E001**) y no puede declararse dos
veces en el mismo ámbito (**E002**).

#### 5.1.1 Bloques

Cada bloque `> ... <` abre un ámbito anidado en el que lo contiene. Lo
declarado dentro de un bloque deja de existir al cerrarlo.

#### 5.1.2 La sesión

El cuerpo de `session` forma un ámbito propio. TM no tiene variables globales:
las variables de `session` no son visibles desde las funciones.

#### 5.1.3 Funciones

Los parámetros y el cuerpo de una función comparten un único ámbito, de modo
que una variable del cuerpo no puede llamarse igual que un parámetro
(**E002**). Una función solo accede a sus parámetros y a sus variables locales.

#### 5.1.4 Bucles `loop`

La variable declarada en la cabecera de `loop`, la condición, la actualización
y el cuerpo comparten un ámbito que termina con el bucle.

#### 5.1.5 Sombreado

Declarar en un ámbito interno un nombre ya declarado en un ámbito externo es
válido: el nombre interno oculta al externo mientras dura su ámbito. El
compilador lo advierte con **W002**.

#### 5.1.6 Nombres sin uso

Una variable o constante que se declara y nunca se lee produce **W001**. Los
parámetros no se advierten, porque forman parte de la firma.

### 5.2 Constantes

El inicializador de una constante debe poder evaluarse en tiempo de
compilación (**E018**). Son expresiones constantes los literales, los
operadores unarios y binarios aplicados a expresiones constantes y los literales
de colección cuyos elementos son constantes. No lo son los identificadores (ni
siquiera los de otras constantes), las llamadas, las indexaciones ni
`length()`.

```tm
const sample SEGUNDOS = 60 * 60;   # válido
const sample DOBLE = SEGUNDOS * 2; # E018: usa un identificador
```

### 5.3 Funciones

**Espacio de nombres.** Las funciones comparten un único espacio de nombres
por proyecto. Dos funciones no pueden tener el mismo nombre, aunque difieran en
sus parámetros: TM no admite sobrecarga (**E013**).

**Declaración y uso.** Una función puede invocarse desde cualquier punto del
archivo, también antes de su declaración: el analizador recolecta todas las
firmas antes de analizar los cuerpos. Se admite la recursión directa e
indirecta.

**Llamadas.** Una llamada debe pasar tantos argumentos como parámetros declara
la función (**E007**) y cada argumento debe ser asignable al tipo de su
parámetro (**E008**). Llamar a una función inexistente produce **E001**, o
**E020** si está declarada en un archivo del proyecto que no se importa.
Una función que nunca se invoca produce **W003**.

**Paso de argumentos.** Los argumentos se pasan por valor: reasignar un
parámetro no afecta a la variable del llamador. Como el valor de una colección
es una referencia (sección 4.2), modificar sus elementos dentro de la función
sí es visible para el llamador.

**Retornos.** Se aplican estas reglas (**E006**):

- Una función `void` no puede devolver un valor; termina al final de su cuerpo o
  con `ret;`.
- Una función con tipo de retorno no puede usar `ret;` sin expresión, y el valor
  devuelto debe ser asignable a ese tipo (**E003** en caso contrario).
- Una función con tipo de retorno debe devolver un valor en todas las
  trayectorias de ejecución. El análisis es estructural: un bloque garantiza el
  retorno si contiene un `ret`, un `if` con `else` cuyas ramas garantizan todas
  el retorno o un `mix` con `default` cuyas cláusulas lo garantizan todas. Los
  bucles `loop` y `repeat` nunca garantizan el retorno.

**Uso del resultado.** Una función `void` no produce un valor y no puede
usarse dentro de una expresión (**E004**). El resultado de una función con tipo
de retorno puede descartarse invocándola como sentencia.

### 5.4 Entrada y salida

- `output >> expresión;` admite expresiones de cualquier tipo primitivo o
  `silence`. Escribir una colección produce **E004**.
- `input << nombre;` exige que `nombre` sea una variable declarada (**E001**),
  modificable (**E009**) y de tipo primitivo (**E004**).

### 5.5 Selección múltiple `mix`

- La expresión de control es de un tipo primitivo (**E004**).
- El valor de cada `case` debe ser del mismo tipo que la expresión de control
  (**E004**).
- Dos `case` con el mismo literal producen **E004**.
- Cada cláusula es un bloque independiente y no existe la caída de una
  cláusula a la siguiente.

### 5.6 Condiciones

Las condiciones de `if`, `else if`, `repeat` y `loop` deben ser de tipo
`signal` (**E005**).

### 5.7 Actualización con `++` y `--`

Los operadores de actualización solo se aplican a variables de tipo `sample`
(**E004**), tanto en una sentencia (`i++;`) como en la cabecera de `loop`.

### 5.8 `tempo` y `pulse`

- El valor de `tempo` es un entero entre 64 y 300 BPM (**E040**).
- La intensidad de `pulse` es de tipo `sample` o `wave` (**E042**).

---

## 6. Programas y proyectos

### 6.1 Archivos fuente

| Extensión | Papel | Contenido |
|---|---|---|
| `.tmpr` | Archivo principal del proyecto | Importaciones, `tempo`, funciones y `session`. |
| `.tm` | Módulo | Importaciones y funciones. |

### 6.2 Estructura de un archivo

Cada archivo sigue el orden de la sección 3.1. Un elemento fuera de lugar, como
una declaración `tempo` después de una función o una función después de
`session`, produce **E039**. Una declaración de función sin tipo de retorno
produce **E030**.

```tm
import "utilidades.tm";          # 1. Importaciones

tempo 96;                        # 2. Ritmo del proyecto (opcional)

sample : compose doble(sample x) >   # 3. Funciones
    ret x * 2;
<

session >                        # 4. Punto de entrada
    output >> doble(21);
<
```

### 6.3 La sesión

`session` es el punto de entrada del programa. El archivo `.tmpr` declara
exactamente una sesión: su ausencia o su repetición producen **E011**. Un
módulo `.tm` no puede declarar `session` (**E015**).

La sesión se analiza como el cuerpo de una función `void`: admite `ret;`, que
termina el programa sin ejecutar las sentencias restantes, y rechaza `ret` con
un valor (**E006**).

### 6.4 La declaración `tempo`

`tempo` fija el ritmo del proyecto en pulsos por minuto. Es opcional; sin ella
el ritmo es de 128 BPM. Solo puede aparecer en el archivo `.tmpr` y una sola
vez: una declaración en un módulo o una segunda declaración producen **E041**.
El ritmo no altera el resultado del programa; lo consumen los entornos que
visualizan la ejecución (sección 11.3).

### 6.5 Importaciones

```tm
import "utilidades.tm";
import "lib/matematicas.tm";
```

- La ruta es una cadena relativa al directorio del archivo que importa.
- Un archivo inexistente produce **E014**.
- Las importaciones no pueden formar ciclos, directos ni indirectos (**E012**).
  El diagnóstico muestra la cadena completa, por ejemplo
  `a.tm -> b.tm -> a.tm`.
- La importación es textual y transitiva: un archivo ve las funciones de los
  archivos que importa y las de los que estos importan a su vez.
- Solo se importan funciones. Las importaciones no exponen variables, constantes
  ni la sesión.
- Invocar una función declarada en un archivo del proyecto que no es visible
  desde el archivo actual produce **E020**, con la indicación del archivo que
  debe importarse.
- Si dos archivos declaran una función con el mismo nombre, el conflicto
  (**E013**) se informa en el archivo que se analiza en segundo lugar según el
  orden de dependencias.

### 6.6 Proyectos

Un proyecto es el conjunto de archivos que se compilan juntos. Se abre de una
de dos formas:

**Desde un directorio.** Forman el proyecto todos los archivos `.tm` y `.tmpr`
del directorio (sin incluir subdirectorios) más los que estos importen. El
directorio contiene exactamente un archivo `.tmpr`: ninguno produce **E016** y
cada archivo adicional, **E017**. Ese archivo es la entrada del programa.

**Desde un archivo.** El archivo indicado es la entrada y el proyecto se
completa con los archivos que importa. Las reglas de cantidad de archivos
`.tmpr` no se aplican.

Estructura típica:

```text
mi-proyecto/
├── principal.tmpr
├── utilidades.tm
└── lib/
    └── matematicas.tm
```

Un proyecto es **ejecutable** cuando no tiene errores y su archivo de entrada
es un `.tmpr` con `session`. Las advertencias no impiden la ejecución.

---

## 7. Diagnósticos

### 7.1 Formato

Todo diagnóstico, de compilación o de ejecución, se presenta como un bloque de
cinco campos:

```text
[ERROR E003] Tipo incompatible en asignacion
Archivo: principal.tmpr
Línea: 2
Descripción: No se puede asignar un valor de tipo 'wave' a la variable 'x', de tipo 'sample'.
Posible solución: Use una función de conversión o cambie el tipo declarado.
```

Las advertencias usan la etiqueta `ADVERTENCIA` en lugar de `ERROR`. El título
de cada código es fijo y procede del catálogo; la descripción detalla el caso
concreto.

| Prefijo | Clase | Efecto |
|---|---|---|
| `E` | Error de compilación | Impide ejecutar el proyecto. |
| `R` | Error de ejecución | Detiene el programa en curso. |
| `W` | Advertencia | Se informa sin impedir la ejecución. |

**Modelo de recuperación.** Un error léxico o sintáctico detiene el análisis
del archivo afectado, que informa ese único error con su línea y su columna.
Los demás archivos del proyecto se siguen analizando. El análisis semántico
acumula todos los errores y advertencias de cada archivo, con su línea. Los
diagnósticos se presentan ordenados por archivo y línea.

### 7.2 Errores de compilación

#### 7.2.1 Errores semánticos y de proyecto

| Código | Título | Causa |
|---|---|---|
| E001 | Identificador no declarado | Uso de una variable o función que no existe. |
| E002 | Redeclaración de identificador en el mismo ámbito | Dos declaraciones con el mismo nombre en un ámbito, o un parámetro repetido. |
| E003 | Tipo incompatible en asignación | El valor no es asignable al destino. |
| E004 | Tipo incompatible en operación | Operador, índice, `length()`, `mix`, `input`, `output` o uso de `void` no válidos para los tipos implicados. |
| E005 | Condición no booleana en if/repeat/loop | La condición no es `signal`. |
| E006 | Función void retorna valor / función no-void no retorna valor | Incumplimiento de las reglas de retorno. |
| E007 | Número incorrecto de argumentos en llamada | La aridad de la llamada no coincide con la firma. |
| E008 | Tipos de argumentos incompatibles en llamada | Un argumento no es asignable a su parámetro. |
| E009 | Acceso a variable const en escritura | Escritura sobre una constante. |
| E010 | Uso de silence en tipo no permitido | `silence` asignado a un tipo distinto de `phrase`, o usado como tipo. |
| E011 | Session ausente o múltiple | El `.tmpr` no tiene `session` o tiene más de una. |
| E012 | Importación cíclica detectada | Ciclo en el grafo de importaciones. |
| E013 | Sobrecarga de función no permitida | Dos funciones con el mismo nombre en el proyecto. |
| E014 | Archivo importado no encontrado | La ruta de `import` no existe. |
| E015 | Archivo .tm contiene session | Un módulo declara `session`. |
| E016 | Proyecto sin archivo .tmpr | El directorio del proyecto no tiene archivo principal. |
| E017 | Proyecto con múltiples archivos .tmpr | El directorio tiene más de un archivo principal. |
| E018 | Constante inicializada con expresión no constante | El inicializador de `const` no es una expresión constante. |
| E019 | Matriz con filas de distinto tamaño | Las filas de un literal `score` no tienen las mismas columnas. |
| E020 | Forward reference a función no resuelta | Llamada a una función de un archivo que no se importa. |

#### 7.2.2 Errores léxicos

| Código | Título | Causa |
|---|---|---|
| E021 | Carácter no válido | Carácter fuera del alfabeto léxico. |
| E022 | Literal de cadena no cerrado | Falta la comilla de cierre antes del fin de línea o de archivo. |
| E023 | Literal de carácter mal formado | Vacío, con varios caracteres, sin cerrar o con un salto de línea. |
| E024 | Secuencia de escape no válida | Escape no admitido o incompleto. |
| E025 | Comentario de bloque no cerrado | Falta el `##` de cierre. |
| E026 | Literal numérico mal formado | Punto decimal sin dígitos a continuación. |

#### 7.2.3 Errores sintácticos

| Código | Título | Causa |
|---|---|---|
| E027 | Se esperaba ';' al final de la sentencia | Falta el punto y coma. |
| E028 | Delimitador de bloque esperado | Falta el `>` de apertura o el `<` de cierre. |
| E029 | Condición sin corchetes | Falta `[` o `]` en una condición, en la cabecera de `loop` o en `pulse`. |
| E030 | Declaración de función mal formada | Falta el tipo de retorno, `:`, `compose` o el nombre. |
| E031 | Lista de parámetros mal formada | Paréntesis, tipos o nombres de parámetros incorrectos. |
| E032 | Se esperaba una expresión | Expresión ausente o miembro distinto de `length()`. |
| E033 | Agrupación sin cerrar | Falta `)` o `]` en una agrupación, llamada, índice o colección. |
| E034 | Declaración de variable o constante mal formada | Tipo, nombre o `=` incorrectos. |
| E035 | Sentencia de entrada/salida mal formada | Falta `>>` en `output`, `<<` en `input` o el identificador de destino. |
| E036 | Cabecera de 'loop' mal formada | La cabecera no empieza con una declaración o la actualización no es válida. |
| E037 | Sentencia 'mix' mal formada | Cláusulas, `:` o delimitadores incorrectos, o más de un `default`. |
| E038 | Sentencia no válida | El texto no corresponde a ninguna sentencia. |
| E039 | Elemento inesperado en el nivel superior | Elemento fuera del orden de la sección 3.1. |

#### 7.2.4 Errores de `tempo` y `pulse`

| Código | Título | Causa |
|---|---|---|
| E040 | Tempo fuera de rango | El valor de `tempo` no está entre 64 y 300 BPM. |
| E041 | Declaración 'tempo' no permitida | `tempo` en un módulo `.tm` o repetida en el `.tmpr`. |
| E042 | Intensidad de 'pulse' no numérica | La intensidad no es `sample` ni `wave`. |

### 7.3 Errores de ejecución

Un error de ejecución detiene el programa. El diagnóstico indica el archivo
donde se produjo: el de la función en curso o, si ocurrió en la sesión, el
archivo principal.

| Código | Título | Causa | Posible solución |
|---|---|---|---|
| R001 | División por cero | Divisor `0` o `0.0`. | Verificar que el divisor sea distinto de cero. |
| R002 | Índice fuera de límites | Índice negativo o mayor o igual que el tamaño. | Verificar que el índice esté dentro de la colección. |
| R003 | Desbordamiento aritmético | Resultado `sample` fuera del rango de 32 bits. | Usar valores dentro del rango del tipo. |
| R004 | Error de parseo en input | El texto leído no representa un valor del tipo de la variable. | Introducir un valor compatible con el tipo. |
| R005 | Llamada a función no definida | La función no existe en tiempo de ejecución. | Verificar que la función esté declarada. |
| R006 | Lectura desde entrada estándar sin datos disponibles (EOF) | `input` sin más datos de entrada. | Proporcionar los datos necesarios. |
| R007 | Línea vacía o inválida para input de tone | La línea leída no contiene ningún carácter visible. | Introducir un carácter. |

### 7.4 Advertencias

| Código | Título | Causa |
|---|---|---|
| W001 | Variable declarada pero no utilizada | Variable o constante que nunca se lee. |
| W002 | Sombreado de identificador (shadowing) | Declaración que oculta un nombre de un ámbito externo. |
| W003 | Función declarada pero nunca invocada | Ningún archivo del proyecto invoca la función. |
| W004 | Conversión implícita en concatenación phrase | `+` entre un `phrase` y otro tipo primitivo. |

---

## 8. Programas de ejemplo

### 8.1 Factorial iterativo con lectura de datos

```tm
sample : compose factorial(sample n) >
    sample resultado = 1;
    loop [sample i = 2; i <= n; i++] >
        resultado = resultado * i;
    <
    ret resultado;
<

session >
    sample n = 0;
    output >> "Número: ";
    input << n;
    output >> n + "! = " + factorial(n) + "\n";
<
```

### 8.2 Selección múltiple y bucle condicional

```tm
session >
    sample dia = 1;
    repeat [dia <= 3] >
        mix dia >
            case 1: >
                output >> "Lunes\n";
            <
            case 2: >
                output >> "Martes\n";
            <
            default: >
                output >> "Otro día\n";
            <
        <
        dia++;
    <
<
```

### 8.3 Colecciones

```tm
sample : compose sumar(sequence<sample> valores) >
    sample total = 0;
    loop [sample i = 0; i < valores.length(); i++] >
        total = total + valores[i];
    <
    ret total;
<

session >
    sequence<sample> datos = [4, 8, 15, 16, 23, 42];
    score<sample> tablero = [[1, 0], [0, 1]];

    tablero[0][1] = 7;
    output >> "Suma: " + sumar(datos) + "\n";
    output >> "Celda: " + tablero[0][1] + "\n";
<
```

### 8.4 Proyecto con módulos

`utilidades.tm`:

```tm
signal : compose esPar(sample n) >
    ret (n / 2) * 2 == n;
<
```

`principal.tmpr`:

```tm
import "utilidades.tm";

tempo 110;

session >
    loop [sample i = 1; i <= 4; i++] >
        if [esPar(i)] >
            output >> i + " es par\n";
            pulse [i * 20];
        <
    <
<
```

---

## 9. Semántica de ejecución

El intérprete ejecuta únicamente proyectos ejecutables (sección 6.6). El
programa ejecutado es la sesión del archivo principal junto con las funciones
de todos los archivos visibles desde él.

### 9.1 Evaluación de expresiones

- Los operandos se evalúan de izquierda a derecha.
- `and` y `or` evalúan en cortocircuito: el operando derecho no se evalúa si el
  izquierdo ya determina el resultado.
- La aritmética `sample` es entera y comprobada: la división trunca hacia cero
  (`7 / 2` es `3`, `-7 / 2` es `-3`) y cualquier resultado fuera del rango de
  32 bits produce **R003**, también en el `-` unario y en `++`/`--`.
- La aritmética `wave` sigue la norma IEEE 754.
- Dividir entre `0` o `0.0` produce **R001**.
- La igualdad compara valores del mismo tipo: los números por valor, los
  `phrase` carácter a carácter y dos `silence` son siempre iguales.

### 9.2 Variables y colecciones

- Una declaración evalúa su inicializador y crea la variable en el ámbito
  actual.
- Indexar fuera de los límites, en lectura o en escritura, produce **R002**.
- Un literal de colección crea una colección nueva cada vez que se evalúa.
  Asignar o pasar una colección comparte la misma colección (sección 4.2).

### 9.3 Sentencias de control

| Sentencia | Ejecución |
|---|---|
| `if` | Evalúa las condiciones en orden y ejecuta el bloque de la primera verdadera; si ninguna lo es, ejecuta el `else`, si existe. |
| `repeat [c]` | Mientras `c` sea verdadera, ejecuta el cuerpo. La condición se evalúa antes de cada iteración. |
| `loop [decl; c; act]` | Ejecuta la declaración una vez; después, mientras `c` sea verdadera, ejecuta el cuerpo y a continuación la actualización. |
| `mix e` | Evalúa `e` y la compara con cada `case` en orden; ejecuta el bloque del primero que coincide y termina. Si ninguno coincide, ejecuta `default`, si existe. |
| `ret` | Termina la función en curso y, si lleva expresión, devuelve su valor. En `session`, `ret;` termina el programa con normalidad. |

### 9.4 Funciones

Cada llamada evalúa sus argumentos en el ámbito del llamador, de izquierda a
derecha, y ejecuta el cuerpo en un ámbito nuevo que contiene solo los
parámetros. La recursión no tiene un límite propio del lenguaje.

### 9.5 Entrada y salida

`output` escribe la representación textual del valor (sección 4.4.1.2; un
`phrase` se escribe tal cual y `silence` como `silence`). No añade un salto de
línea: para terminar una línea se escribe `"\n"`.

`input` lee una línea completa de la entrada y la convierte al tipo declarado
de la variable:

| Tipo | Conversión | Error |
|---|---|---|
| `sample` | Entero decimal; se ignoran los espacios de los extremos. | R004 |
| `wave` | Número real con punto decimal; se ignoran los espacios de los extremos. | R004 |
| `phrase` | La línea completa, sin modificar. | — |
| `signal` | `true` o `false`, sin distinguir mayúsculas. | R004 |
| `tone` | El primer carácter que no es espacio en blanco. | R007 |

Si la entrada no tiene más líneas, `input` produce **R006**.

### 9.6 Ráfagas `pulse`

`pulse [i];` evalúa la intensidad `i` y la limita al intervalo de 0 a 100. Si
el resultado es mayor que 0, emite una ráfaga con esa intensidad hacia el
entorno de ejecución; si es 0 o negativo, no emite nada. Las ráfagas no
alteran el estado ni la salida del programa.

---

## 10. Casos de verificación

Programas de referencia que toda implementación de TM debe procesar con el
resultado indicado. La salida se muestra tal como la produce `output`, sin
saltos de línea añadidos.

### 10.1 Programas válidos

| Caso | Programa (cuerpo de `session` salvo indicación) | Resultado |
|---|---|---|
| V1 | `sample a = 10; sample b = 3;` y salida de `"Suma: " + (a + b)`, `"Resta: " + (a - b)`, `"Mult: " + (a * b)`, `"Div: " + (a / b)` | `Suma: 13Resta: 7Mult: 30Div: 3` |
| V2 | `if [x > 10]` anidado con `if [x < 20]`, con `x = 15` | `Entre 10 y 20` |
| V3 | Función recursiva `fibonacci` y `output >> "Fib(7) = " + fibonacci(7);` | `Fib(7) = 13` |
| V4 | `sequence<sample> datos = [10, 20, 30];`, lectura de `datos[0]`, escritura `datos[1] = 40;` y `datos.length()` | `10403` |
| V5 | `const sample LIMITE = 100; const phrase ETIQUETA = "El limite es: ";` y `output >> ETIQUETA + LIMITE;` | `El limite es: 100`, con **W004** |

### 10.2 Errores sintácticos

| Caso | Fuente | Diagnóstico |
|---|---|---|
| S1 | `sample x = 10  # Falta ;` dentro de `session` | E027, línea 2 |
| S2 | `session <` ... `>` (delimitadores invertidos) | E028, línea 1 |
| S3 | `if x > 0 >` (condición sin corchetes) | E029, línea 2 |
| S4 | `compose funcion() >` (sin tipo de retorno) | E030, línea 1 |

### 10.3 Errores semánticos

Cada caso produce exactamente un error, con el código indicado.

| Caso | Fuente | Diagnóstico |
|---|---|---|
| T1 | `sample x = 3.14;` | E003 |
| T2 | `sample x = 5;` seguido de `if [x] > ... <` | E005 |
| T3 | `output >> y;` sin declarar `y` | E001 |
| T4 | Dos funciones `sumar` en el mismo archivo | E013 |
| T5 | `const sample X = 10;` seguido de `X = 20;` | E009 |

### 10.4 Importaciones

| Caso | Proyecto | Resultado |
|---|---|---|
| I1 | `principal.tmpr` importa un módulo que declara `doble` e imprime `doble(5)` | Salida `10` |
| I2 | `principal.tmpr` llama a una función que el módulo importado no declara | E001 en `principal.tmpr` |
| I3 | `principal.tmpr` pasa un argumento de tipo incompatible a una función importada | E008 en `principal.tmpr` |

### 10.5 Ciclos de importación

| Caso | Proyecto | Resultado |
|---|---|---|
| C1 | `a.tm` importa `b.tm` y `b.tm` importa `a.tm` | E012 con la cadena del ciclo |

---

## 11. Interfaz de ejecución

El intérprete no accede directamente a la consola: se comunica con el entorno
que lo aloja (el *host*) mediante tres contratos. Cada host los implementa
según su medio.

### 11.1 Entrada

`IRuntimeInput.ReadLine()` devuelve la siguiente línea de entrada, o un valor
nulo si no hay más datos. Lo invoca `input <<`.

### 11.2 Salida

`IRuntimeOutput.Write(texto)` recibe el texto que produce cada `output >>`, sin
saltos de línea añadidos.

### 11.3 Ráfagas

`IRuntimePulse.Emit(intensidad)` recibe cada ráfaga de `pulse`, con una
intensidad en el intervalo (0, 100]. Un host sin representación visual puede
descartarlas.

### 11.4 Cancelación

El host puede detener un programa en ejecución. La detención se produce antes
de ejecutar la siguiente sentencia o la siguiente iteración de un bucle, sin
informar un error del programa.

---

## 12. Implementación de referencia

### 12.1 Fases del compilador

| Fase | Entrada | Salida | Diagnósticos |
|---|---|---|---|
| 1. Análisis léxico | Texto fuente | Secuencia de tokens | E021–E026 |
| 2. Análisis sintáctico | Tokens | Árbol de sintaxis abstracta | E027–E039 |
| 3. Análisis semántico | Árbol de cada archivo y firmas visibles | Tabla de símbolos y diagnósticos | E001–E011, E013, E018–E020, E040, E042, W001–W004 |
| 4. Análisis de proyecto | Archivos del proyecto | Grafo de importaciones y reglas estructurales | E012–E017, E041 |
| 5. Interpretación | Árbol del programa ejecutable | Efectos de entrada, salida y ráfagas | R001–R007 |

Las fases 1 y 2 se ejecutan por archivo; la fase 4 resuelve las importaciones
antes de la fase 3, que analiza los archivos en orden de dependencias para que
cada uno conozca las firmas de los que importa. La arquitectura de la
implementación se describe en [ARQUITECTURA.md](ARQUITECTURA.md).

### 12.2 Herramientas

| Herramienta | Uso |
|---|---|
| `Tempo.Cli` | Compila y ejecuta un proyecto desde la línea de comandos. |
| `TmIde` | Entorno de desarrollo gráfico con edición, diagnósticos en vivo y ejecución integrada. |
