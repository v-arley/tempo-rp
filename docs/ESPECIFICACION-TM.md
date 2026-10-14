

# ESPECIFICACION FORMAL DEL LENGUAJE TM v1.1


**Proyecto:** Paradigmas de Programacion — UNA Sede Regional Brunca
**Autor:** Equipo de desarrollo TM

---

## 1. INTRODUCCION

TM es un lenguaje de programacion de proposito general con sintaxis inspirada en
notacion musical. Su diseno prioriza la claridad visual mediante el uso de
palabras clave tematicas (sample, wave, tone, phrase, signal, silence) y una
estructura de bloques explicita mediante delimitadores visuales.

### 1.1 Extensiones de archivo

| Extension | Proposito | Reglas |
| --- | --- | --- |
| `.tm` | Modulo | Contiene importaciones y funciones. No puede contener `session` ni `tempo`. Puede ser importado por otros archivos. |
| `.tmpr` | Archivo principal del proyecto | Contiene obligatoriamente la unica sentencia `session` del proyecto y, opcionalmente, la declaracion `tempo`. Es el punto de entrada de ejecucion. Un proyecto solo puede tener un archivo `.tmpr`. |

### 1.2 Codificacion

El codigo fuente debe estar codificado en UTF-8.

### 1.3 Indentacion

**La indentacion NO es significativa en TM.** La estructura de bloques se define
exclusivamente mediante los delimitadores `>` (inicio de bloque) y `<` (fin de bloque).
El uso de espacios o tabulaciones para alinear codigo es puramente cosmetico y no
afecta la semantica del programa. El compilador ignora todo whitespace que no actue
como separador de tokens.

---

## 2. ANALISIS LEXICO

### 2.1 Estructura del archivo fuente

Un programa TM es una secuencia de caracteres Unicode organizada en lineas.
El analisis lexico agrupa estos caracteres en *tokens*, descartando comentarios
y espacios en blanco no significativos.

### 2.2 Comentarios

| Tipo | Notacion | Descripcion |
| --- | --- | --- |
| Linea | `# ...` | Todo desde `#` hasta el fin de linea es ignorado |
| Bloque | `## ... ##` | Todo entre pares de `##` es ignorado; puede abarcar multiples lineas |

**Notas:**
- Los comentarios no pueden anidarse. Un comentario de bloque termina en el primer `##` encontrado.
- Los comentarios de bloque pueden aparecer en cualquier posicion donde un token sea valido.
- Los comentarios de linea terminan obligatoriamente al final de la linea fisica.

### 2.3 Espacios en blanco

Los caracteres de espacio (U+0020), tabulacion (U+0009), retorno de carro
(U+000D) y nueva linea (U+000A) son *whitespace*. El whitespace separa tokens
cuando es necesario para evitar ambiguedad, pero no es significativo en ningun
otro contexto. No existe indentacion significativa; la estructura se define
mediante delimitadores explicitos.

### 2.4 Tokens

#### 2.4.1 Palabras reservadas

Las siguientes palabras son reservadas y no pueden usarse como identificadores.
Distinguen mayusculas y minusculas.

```
compose    session    output     input      mix
loop       repeat     if         else       ret
import     const      sequence   score      true
false      silence    sample     wave       tone
phrase     signal     void       case       default
tempo      pulse
```

#### 2.4.2 Identificadores

```
identifier = letter, { letter | digit | "_" } ;
letter     = "a".."z" | "A".."Z" ;
digit      = "0".."9" ;
```

- Distingue mayusculas y minusculas.
- No puede comenzar con digito ni guion bajo.
- Longitud maxima recomendada: 255 caracteres.

#### 2.4.3 Literales numericos

**Enteros (`sample`):**

```
integer_literal = [ "-" ], digit, { digit } ;
```

- Ejemplos validos: `0`, `42`, `17`
- No se permite notacion octal ni hexadecimal.
- Rango: 32 bits con signo [-2.147.483.648, 2.147.483.647].

**Flotantes (`wave`):**

```
float_literal = [ "-" ], digit, { digit }, ".", digit, { digit } ;
```

- Ejemplos validos: `3.14`, `0.5`, `10.0`
- Debe haber al menos un digito antes y despues del punto decimal.
- No se permite notacion cientifica.
- Precision simple IEEE 754.

#### 2.4.4 Literales de cadena (`phrase`)

```
string_literal = "\"" , { character | escape_sequence }, "\"" ;
character      = cualquier caracter Unicode excepto "\"", "\\", nueva linea ;
escape_sequence= "\\n" | "\\t" | "\\\"" | "\\\\" ;
```

- Delimitados por comillas dobles.
- Soportan secuencias de escape:
    - `\n` : nueva linea (U+000A)
    - `\t` : tabulacion horizontal (U+0009)
    - `\"` : comilla doble literal (U+0022)
    - `\\` : barra invertida literal (U+005C)

#### 2.4.5 Literales de caracter (`tone`)

```
char_literal = "'", ( character | escape_sequence ), "'" ;
```

- Delimitados por comillas simples.
- Debe contener exactamente un caracter o una secuencia de escape valida.
- Ejemplos validos: `A`, `7`, `\n`, `\t`
- Ejemplos invalidos: vacio, multiple caracteres, escape no valido

#### 2.4.6 Literales booleanos

```
bool_literal = "true" | "false" ;
```

#### 2.4.7 Literal nulo

```
null_literal = "silence" ;
```

#### 2.4.8 Operadores y simbolos de puntuacion

| Simbolo | Categoria | Uso |
| --- | --- | --- |
| `+` | Operador aritmetico / concatenacion | Suma, concatenacion de phrase |
| `-` | Operador aritmetico / unario | Resta, negacion |
| `*` | Operador aritmetico | Multiplicacion |
| `/` | Operador aritmetico | Division |
| `==` | Operador relacional | Igualdad |
| `!=` | Operador relacional | Diferencia |
| `>` | Operador relacional / Delimitador | Mayor que, inicio de bloque |
| `<` | Operador relacional / Delimitador | Menor que, fin de bloque |
| `>=` | Operador relacional | Mayor o igual |
| `<=` | Operador relacional | Menor o igual |
| `=` | Operador de asignacion | Asignacion |
| `and` | Operador logico | Conjuncion |
| `or` | Operador logico | Disyuncion |
| `not` | Operador logico / unario | Negacion |
| `++` | Operador de actualizacion | Incremento unitario |
| `--` | Operador de actualizacion | Decremento unitario |
| `(` `)` | Agrupacion / parametros | Agrupacion de expresiones, lista de parametros |
| `[` `]` | Indexacion / condiciones | Acceso a elementos, condicionales, bucles |
| `{` `}` | Casos de mix | Cuerpo de casos en sentencia mix |
| `:` | Separador | Separador en declaracion de funcion |
| `;` | Terminador | Fin de sentencia |
| `,` | Separador | Separacion de elementos en listas |
| `.` | Acceso | Acceso a propiedades/metodos |
| `>>` | Operador de salida | Envio a consola |
| `<<` | Operador de entrada | Recepcion desde consola |

---

## 3. ANALISIS SINTACTICO

### 3.1 Gramatica EBNF completa

```ebnf

(* ARCHIVOS                                                     *)
project_file   = { import_stmt }, [ tempo_decl ], { function_decl }, session ;  (* .tmpr *)
module_file    = { import_stmt }, { function_decl } ;                           (* .tm   *)

(* IMPORTACIONES                                                *)
import_stmt    = "import", string_literal, ";" ;

(* RITMO DEL PROYECTO (seccion 11)                              *)
tempo_decl     = "tempo", integer_literal, ";" ;

(* PUNTO DE ENTRADA                                             *)
session        = "session", block ;

(* FUNCIONES                                                    *)
function_decl  = type, ":", "compose", identifier,
                 "(", [ param_list ], ")", block ;
param_list     = parameter, { ",", parameter } ;
parameter      = type, identifier ;

(* BLOQUES Y SENTENCIAS                                         *)
block          = ">", { statement }, "<" ;

statement      = var_decl
               | const_decl
               | assign_stmt
               | output_stmt
               | input_stmt
               | if_statement
               | mix_stmt
               | loop_statement
               | repeat_statement
               | ret_stmt
               | pulse_stmt
               | expr_stmt
               ;

(* DECLARACIONES                                                *)
var_decl       = type, identifier, "=", expression, ";" ;
const_decl     = "const", type, identifier, "=", expression, ";" ;

assign_stmt    = identifier, "=", expression, ";"
               | identifier, "[", expression, "]", "=", expression, ";"
               | identifier, "[", expression, "]", "[", expression, "]", "=", expression, ";"
               ;

(* ENTRADA / SALIDA                                             *)
output_stmt    = "output", ">>", expression, ";" ;
input_stmt     = "input", "<<", identifier, ";" ;

(* SENTENCIAS DE CONTROL                                        *)
if_statement   = "if", "[", expression, "]", block,
                 { "else", "if", "[", expression, "]", block },
                 [ "else", block ] ;

mix_stmt       = "mix", expression, "{",
                 { "case", expression, ":", block },
                 [ "default", ":", block ],
                 "}" ;

loop_statement = "loop", "[",
                 var_decl, expression, ";", update_expr,
                 "]", block ;

repeat_statement = "repeat", "[", expression, "]", block ;

(* RETORNO                                                      *)
ret_stmt       = "ret", [ expression ], ";" ;

(* PULSO VISUAL (seccion 11)                                    *)
pulse_stmt     = "pulse", "[", expression, "]", ";" ;

(* EXPRESIONES                                                  *)
expression     = or_expr ;

or_expr        = and_expr, { "or", and_expr } ;
and_expr       = equality_expr, { "and", equality_expr } ;
equality_expr  = relational_expr, { ( "==" | "!=" ), relational_expr } ;
relational_expr= additive_expr, { ( ">" | "<" | ">=" | "<=" ), additive_expr } ;
additive_expr  = multiplicative_expr, { ( "+" | "-" ), multiplicative_expr } ;
multiplicative_expr = unary_expr, { ( "*" | "/" ), unary_expr } ;
unary_expr     = ( "not" | "-" ), unary_expr | primary_expr ;

primary_expr   = literal
               | identifier
               | identifier, "(", [ arg_list ], ")"
               | identifier, "[", expression, "]"
               | identifier, "[", expression, "]", "[", expression, "]"
               | "(", expression, ")"
               ;

arg_list       = expression, { ",", expression } ;
update_expr    = identifier, ( "++" | "--" )
               | identifier, "=", expression
               ;

(* TIPOS                                                        *)
type           = primitive_type
               | composite_type
               ;

primitive_type = "sample" | "wave" | "tone" | "phrase" | "signal" | "silence" ;
composite_type = "sequence", "<", type, ">"
               | "score", "<", type, ">"
               ;

(* LITERALES                                                    *)
literal        = integer_literal
               | float_literal
               | string_literal
               | char_literal
               | bool_literal
               | null_literal
               | array_literal
               | matrix_literal
               ;

array_literal  = "[", [ expression_list ], "]" ;
matrix_literal = "[", array_literal, { ",", array_literal }, "]" ;
expression_list= expression, { ",", expression } ;

(* IDENTIFICADORES                                              *)
identifier     = letter, { letter | digit | "_" } ;
letter         = "a" .. "z" | "A" .. "Z" ;
digit          = "0" .. "9" ;
```

**Tipo de archivo.** La extension determina la produccion que aplica:
`project_file` para `.tmpr` y `module_file` para `.tm`. El analizador sintactico
acepta la union de ambas (`session` y `tempo` opcionales). Las reglas propias de
cada tipo de archivo se verifican en el analisis semantico: E011 (session ausente
o multiple), E015 (`.tm` con `session`) y E041 (`tempo` fuera del `.tmpr` o
duplicado).

### 3.2 Resolucion de ambiguedades sintacticas

#### 3.2.1 Delimitadores `>` y `<` vs. operadores relacionales

Los simbolos `>` y `<` actuan como delimitadores de bloque **unicamente** cuando
aparecen en posiciones sintacticas esperadas:
- Despues de la condicion de `if`, `loop`, `repeat`
- Despues de la lista de parametros de una funcion
- Despues de la palabra `session`
- Despues de `case valor:` o `default:` dentro de `mix`

En cualquier otra posicion (especialmente dentro de expresiones entre `[` y `]`),
se interpretan como operadores relacionales.

**Ejemplo:**

```
if [a > b] >
    output >> "mayor";
<
```

#### 3.2.2 Precedencia y asociatividad de operadores

| Precedencia | Operador(es) | Asociatividad | Descripcion |
| --- | --- | --- | --- |
| 1 (mayor) | `()` `[]` `.` | Izquierda | Agrupacion, indexacion, acceso |
| 2 | `not` `-` (unario) | Derecha | Negacion logica, negacion aritmetica |
| 3 | `*` `/` | Izquierda | Multiplicacion, division |
| 4 | `+` `-` | Izquierda | Suma, resta, concatenacion |
| 5 | `>` `<` `>=` `<=` | Izquierda | Relacionales |
| 6 | `==` `!=` | Izquierda | Igualdad |
| 7 | `and` | Izquierda | Conjuncion logica |
| 8 (menor) | `or` | Izquierda | Disyuncion logica |

---

## 4. SISTEMA DE TIPOS

### 4.1 Tipos primitivos

| Tipo | Descripcion | Valores permitidos | Tamano (implementacion) |
| --- | --- | --- | --- |
| `sample` | Entero con signo de 32 bits | Z en rango [-2.147.483.648, 2.147.483.647] | 32 bits |
| `wave` | Punto flotante de precision simple | R segun IEEE 754 | 32 bits IEEE 754 |
| `tone` | Caracter Unicode | Un unico codigo Unicode | 32 bits (codigo Unicode) |
| `phrase` | Cadena de texto mutable | Secuencia de caracteres Unicode | Referencia dinamica |
| `signal` | Booleano | `true`, `false` | 8 bits |
| `silence` | Nulo | `silence` unico | — |

### 4.2 Tipos compuestos

| Tipo | Descripcion | Sintaxis de declaracion |
| --- | --- | --- |
| `sequence<T>` | Arreglo unidimensional homogeneo de tamano fijo | `sequence<tipo> nombre = [v1, v2, ...];` |
| `score<T>` | Matriz bidimensional homogenea de tamano fijo | `score<tipo> nombre = [[f1c1, f1c2], [f2c1, f2c2]];` |

### 4.3 Mutabilidad

- Toda variable declarada con `const` es **inmutable**. Su valor no puede
modificarse despues de la inicializacion.
- Toda variable declarada sin `const` es **mutable**.
- Los parametros de funciones son inmutables por defecto (paso por valor).

### 4.4 Reglas de tipado

#### 4.4.1 Compatibilidad de tipos

TM es un lenguaje **estaticamente tipado** con **tipado fuerte**. No existe
coercion implicita entre tipos distintos, con las siguientes excepciones
especificas:

1. **Conversion numerica explicita:** No existe. Los tipos `sample` y `wave`
son incompatibles. El programador debe usar funciones de conversion si
se requiere (a definir en biblioteca estandar).
2. **Concatenacion con `phrase`:** El operador `+` entre un `phrase` y cualquier
otro tipo primitivo (`sample`, `wave`, `tone`, `signal`) produce un `phrase`
resultante de la conversion textual del operando no-phrase. Ambos ordenes
son validos: `phrase + sample` y `sample + phrase`.
3. **Asignacion de `silence`:** Una variable de tipo `phrase` puede asignarse
el valor `silence`. Para otros tipos, la asignacion de `silence` es un error
de tipo.

#### 4.4.2 Reglas por operador

| Operador | Tipos operandos | Tipo resultado | Notas |
| --- | --- | --- | --- |
| `+` | `sample` x `sample` | `sample` | Suma entera |
| `+` | `wave` x `wave` | `wave` | Suma flotante |
| `+` | `phrase` x `phrase` | `phrase` | Concatenacion |
| `+` | `phrase` x T (primitivo) | `phrase` | Concatenacion con conversion textual |
| `+` | T (primitivo) x `phrase` | `phrase` | Concatenacion con conversion textual |
| `-` `*` `/` | `sample` x `sample` | `sample` | Aritmetica entera |
| `-` `*` `/` | `wave` x `wave` | `wave` | Aritmetica flotante |
| `==` `!=` | Mismo tipo primitivo | `signal` | Comparacion por valor |
| `>` `<` `>=` `<=` | `sample` x `sample` | `signal` | Comparacion entera |
| `>` `<` `>=` `<=` | `wave` x `wave` | `signal` | Comparacion flotante |
| `and` `or` | `signal` x `signal` | `signal` | Logica booleana |
| `not` | `signal` | `signal` | Negacion booleana |
| `-` (unario) | `sample` | `sample` | Negacion entera |
| `-` (unario) | `wave` | `wave` | Negacion flotante |
| `++` `--` | `sample` (variable) | — | Incremento/decremento unitario |

#### 4.4.3 Reglas de indexacion

- `sequence<T>`: Acceso mediante un unico indice de tipo `sample`.
Indices base 0. Resultado de tipo `T`.
- `score<T>`: Acceso mediante dos indices de tipo `sample` [fila][columna].
Indices base 0. Resultado de tipo `T`.
- Acceso fuera de limites: **Error en tiempo de ejecucion** (runtime error).

#### 4.4.4 Propiedad `length()`

Los tipos `sequence<T>` y `score<T>` exponen una propiedad de solo lectura
`length()` que retorna un `sample` indicando:
- Para `sequence`: cantidad de elementos.
- Para `score`: cantidad de filas (primera dimension).

#### 4.4.5 Operaciones sobre tipos compuestos

**Asignacion entre compuestos:**
- La asignacion de un `sequence` a otro (`seqA = seqB;`) copia la **referencia**.
Ambas variables apuntan al mismo arreglo en memoria.
- La asignacion de un `score` a otro (`matA = matB;`) copia la **referencia**.
- No existe operador de copia profunda en el lenguaje base.

**Comparacion entre compuestos:**
- No se permite comparar dos `sequence` con `==` o `!=`.
- No se permite comparar dos `score` con `==` o `!=`.
- La comparacion debe realizarse elemento por elemento mediante ciclos.

**Concatenacion de secuencias:**
- No existe operador de concatenacion para `sequence` ni `score`.
- El tamano de los compuestos es fijo despues de la creacion.

---

## 5. SEMANTICA ESTATICA

### 5.1 Ambito (Scope)

TM utiliza **ambito lexico estatico** con las siguientes reglas:

1. **Ambito global:** Las funciones declaradas a nivel de programa y las
variables declaradas fuera de cualquier bloque pertenecen al ambito global.
El ambito global es **compartido entre todos los archivos** de un proyecto
despues de resolver las importaciones.
2. **Ambito de bloque:** Cada bloque (`> ... <`) introduce un nuevo ambito.
Las variables declaradas dentro de un bloque son visibles unicamente dentro
de ese bloque y sus bloques anidados.
3. **Ambito de funcion:** Los parametros de una funcion pertenecen al ambito
del cuerpo de la funcion.
4. **Ambito de control:** Las variables declaradas en la inicializacion de
un `loop` pertenecen al ambito del cuerpo del bucle.
5. **Sombreado (shadowing):** Una declaracion en un ambito interno puede
ocultar una declaracion del mismo nombre en un ambito externo. Esto es valido
pero debe generar una advertencia del compilador.

### 5.2 Declaraciones

- **Unica declaracion:** Un identificador no puede declararse mas de una vez
en el mismo ambito.
- **Inicializacion obligatoria:** Toda variable debe inicializarse en su
declaracion. No existen valores por defecto.
- **Constantes:** Las variables `const` deben inicializarse con una expresion
constante evaluable en tiempo de compilacion (literales u operaciones entre
literales).

### 5.3 Funciones

- **Sobrecarga:** No se permite la sobrecarga de funciones. Cada funcion debe
tener un nombre unico en el ambito global del proyecto completo.
- **Recursion:** Se permite la recursion directa e indirecta.
- **Tipo de retorno:** Una funcion declarada con tipo `void` no puede retornar
un valor (solo `ret;`). Una funcion con tipo distinto a `void` debe retornar
un valor del tipo declarado en todas las trayectorias de ejecucion.
- **Paso de parametros:** Por valor. Los cambios a los parametros dentro de
la funcion no afectan a los argumentos originales.
- **Forward declaration:** No es necesaria. Una funcion puede ser invocada
antes de su declaracion en el mismo archivo o en archivos importados.
El compilador realiza dos pasadas: la primera recolecta todas las firmas de
funciones, y la segunda verifica las llamadas.

### 5.4 Sentencia `session`

- Debe existir exactamente una sentencia `session` por proyecto.
- Debe encontrarse obligatoriamente en el archivo `.tmpr`.
- Es el punto de entrada de ejecucion.
- No recibe parametros ni retorna valor.

### 5.5 Sentencia `mix`

- La expresion de control y las expresiones de cada `case` deben ser del
mismo tipo primitivo (`sample`, `wave`, `tone`, `phrase`, `signal`).
- No se permite fall-through entre casos. Cada caso termina implicitamente
al finalizar su bloque.
- El `default` es opcional. Si esta presente, se ejecuta cuando ningun `case`
coincide.

### 5.6 Sentencias de control

- **Condicion de `if` y `repeat`:** Debe ser de tipo `signal`.
- **Condicion de `loop`:** La expresion del medio debe ser de tipo `signal`.
- **Actualizacion de `loop`:** La expresion de actualizacion debe ser una
asignacion valida o un incremento/decremento (`++`/`-`).

### 5.7 Visibilidad entre archivos

- **Todas las funciones son publicas por defecto.** No existen modificadores
de visibilidad. Cualquier funcion definida en un archivo `.tm` puede ser
invocada desde cualquier otro archivo del mismo proyecto tras importarlo.
- **No existen variables globales.** Las variables y constantes solo pueden
declararse dentro de un bloque (`session`, funciones y sentencias de control).
Lo unico que se comparte entre archivos son las funciones.
- **No existen namespaces.** Todos los nombres de funciones comparten un unico
espacio de nombres por proyecto.

---

## 6. SEMANTICA DINAMICA

### 6.1 Modelo de ejecucion

TM sigue un modelo de ejecucion secuencial con pila de llamadas:

1. La ejecucion comienza en la sentencia `session` del archivo `.tmpr`.
2. Las sentencias se ejecutan en orden lexico.
3. Las llamadas a funciones crean un nuevo marco de activacion (stack frame).
4. Al retornar de una funcion, el control vuelve al punto de llamada.
5. El programa termina cuando la ejecucion de `session` concluye.

### 6.2 Comportamiento de operadores

#### 6.2.1 Division

- `sample / sample`: Division entera. El resultado se trunca hacia cero.
- `wave / wave`: Division en punto flotante segun IEEE 754.
- Division por cero: **Error en tiempo de ejecucion**.

#### 6.2.2 Comparacion de `phrase`

- Dos `phrase` son iguales (`==`) si tienen la misma secuencia de caracteres.
- La comparacion es sensible a mayusculas/minusculas.

#### 6.2.3 Comparacion de `wave`

- La comparacion de flotantes (`==`, `!=`) compara los valores exactos de
representacion binaria. No se aplica epsilon de tolerancia.

### 6.3 Comportamiento de tipos compuestos

#### 6.3.1 `sequence<T>`

- Los elementos se almacenan contiguamente en memoria.
- El tamano es fijo despues de la creacion (no se permite redimensionamiento).
- La asignacion de un `sequence` a otro copia la referencia (no los elementos).
- La propiedad `.length()` retorna la cantidad de elementos como `sample`.

#### 6.3.2 `score<T>`

- Las filas deben tener la misma cantidad de columnas.
- El tamano es fijo despues de la creacion.
- La asignacion copia la referencia.
- La propiedad `.length()` retorna la cantidad de filas como `sample`.

### 6.4 Entrada/Salida

#### 6.4.1 `output >> expr;`

- Evalua la expresion y escribe su representacion textual en la salida estandar.
- Para `phrase`: escribe el contenido sin comillas.
- Para `tone`: escribe el caracter sin comillas simples.
- Para `signal`: escribe `true` o `false`.
- Para `silence`: escribe `silence`.
- Para `sample`: escribe la representacion decimal sin ceros a la izquierda.
- Para `wave`: escribe la representacion decimal con al menos un digito despues
del punto (ej: `3.0` en vez de `3`).
- No anade salto de linea automaticamente. Para nueva linea, usar `\n` dentro
de un `phrase`.

#### 6.4.2 `input << var;`

- Lee una linea completa desde la entrada estandar (hasta el caracter de nueva
linea, sin incluirlo).
- El whitespace al inicio y al final de la linea se recorta (trim) antes del
parseo para todos los tipos excepto `phrase`.
- Para `sample`: parsea como entero decimal con signo opcional. Si el parseo
falla o el valor excede el rango: **Error en tiempo de ejecucion**.
- Para `wave`: parsea como flotante decimal con signo opcional. Si el parseo
falla: **Error en tiempo de ejecucion**.
- Para `phrase`: almacena la linea completa (sin el caracter de nueva linea)
tal cual fue leida, sin recortar whitespace.
- Para `tone`: lee el primer caracter no-whitespace de la linea. Si la linea
esta vacia o solo contiene whitespace: **Error en tiempo de ejecucion**.
- Para `signal`: la linea recortada debe ser exactamente `true` o `false`
(insensible a mayusculas/minusculas). Cualquier otro valor: **Error en tiempo
de ejecucion**.
- Si se intenta leer `input` cuando no hay mas datos (EOF): **Error en tiempo
de ejecucion**.

### 6.5 Importaciones (`import`)

- El mecanismo de importacion es textual (similar a `#include` de C): el
contenido del archivo importado se inserta en el punto de la declaracion.
- No existe namespace ni encapsulamiento de modulos. Todos los identificadores
importados comparten el ambito global con el archivo principal.
- Se debe detectar e informar importaciones ciclicas como error.
- El compilador debe validar:
    - Existencia del archivo importado.
    - Que el archivo importado no contenga una sentencia `session`.
    - Que no haya declaraciones duplicadas entre archivos.

### 6.6 Estructura de un proyecto multiarchivo

Un proyecto TM es un directorio que contiene:

```
MiProyecto/
  |-- principal.tmpr      # Archivo principal (obligatorio, unico)
  |-- operaciones.tm      # Modulo de funciones auxiliares
  |-- utilidades.tm       # Otro modulo importado
```

**Reglas del proyecto:**
- Debe existir exactamente un archivo `.tmpr`.
- El archivo `.tmpr` debe contener exactamente una sentencia `session`.
- Los archivos `.tm` no pueden contener `session`.
- Todos los archivos `.tm` y `.tmpr` del directorio del proyecto se consideran
parte del mismo proyecto.
- El compilador procesa todos los archivos del proyecto, resolviendo importaciones
y verificando que no existan declaraciones duplicadas en el ambito global.

---

## 7. ERRORES Y COMPORTAMIENTO DEFINIDO

### 7.1 Formato estandarizado de mensajes de error

Todo mensaje de error producido por el compilador o el interprete debe seguir
el siguiente formato:

```
[<TIPO> <CODIGO>] <Titulo del error>
Archivo: <nombre_archivo.ext>
Línea: <numero_linea>
Descripción: <explicacion detallada del error>
Posible solución: <sugerencia para corregir el error>
```

Las etiquetas `Archivo:`, `Línea:`, `Descripción:` y `Posible solución:` se
escriben literalmente como se muestran, con tilde. El formato es el mismo para
los errores lexicos, sintacticos, semanticos y de ejecucion, y para las
advertencias.

**Tipos de mensaje:**
- `[ERROR E###]` : Error de compilacion que impide la ejecucion.
- `[ERROR R###]` : Error en tiempo de ejecucion.
- `[ADVERTENCIA W###]` : Advertencia que no impide la ejecucion pero indica
un posible problema.

**Ejemplo:**

```
[ERROR E003] Tipo incompatible en asignacion
Archivo: operaciones.tm
Línea: 15
Descripción: No se puede asignar un valor de tipo 'wave' a una variable de tipo 'sample'.
Posible solución: Use una funcion de conversion o cambie el tipo de la variable.
```

### 7.2 Errores de compilacion (tiempo de analisis)

#### 7.2.1 Errores semanticos

| Codigo | Descripcion |
| --- | --- |
| E001 | Identificador no declarado |
| E002 | Redeclaracion de identificador en el mismo ambito |
| E003 | Tipo incompatible en asignacion |
| E004 | Tipo incompatible en operacion |
| E005 | Condicion no booleana en if/repeat/loop |
| E006 | Funcion void retorna valor / funcion no-void no retorna valor |
| E007 | Numero incorrecto de argumentos en llamada |
| E008 | Tipos de argumentos incompatibles en llamada |
| E009 | Acceso a variable const en escritura |
| E010 | Uso de silence en tipo no permitido |
| E011 | Session ausente o multiple |
| E012 | Importacion ciclica detectada |
| E013 | Sobrecarga de funcion no permitida |
| E014 | Archivo importado no encontrado |
| E015 | Archivo .tm contiene session |
| E016 | Proyecto sin archivo .tmpr |
| E017 | Proyecto con multiples archivos .tmpr |
| E018 | Constante inicializada con expresion no constante |
| E019 | Matriz con filas de distinto tamano |
| E020 | Forward reference a funcion no resuelta |

#### 7.2.2 Errores lexicos

El analisis lexico se detiene en el primer error.

| Codigo | Descripcion | Casos que cubre |
| --- | --- | --- |
| E021 | Caracter no valido | Caracter fuera del alfabeto lexico de TM |
| E022 | Literal de cadena no cerrado | Falta `"` antes del fin de linea o del fin de archivo |
| E023 | Literal de caracter mal formado | Vacio, mas de un caracter, salto de linea o falta `'` |
| E024 | Secuencia de escape no valida | Escape distinto de `\n`, `\t`, `\"`, `\\`, o escape incompleto |
| E025 | Comentario de bloque no cerrado | Falta `##` antes del fin de archivo |
| E026 | Literal numerico mal formado | Falta el digito despues del punto decimal |

#### 7.2.3 Errores sintacticos

El analisis sintactico se detiene en el primer error.

| Codigo | Descripcion | Casos que cubre |
| --- | --- | --- |
| E027 | Se esperaba `;` al final de la sentencia | Cualquier sentencia, importacion o declaracion sin terminador |
| E028 | Delimitador de bloque esperado | Falta `>` al iniciar o `<` al cerrar un bloque, o delimitadores invertidos |
| E029 | Condicion sin corchetes | Falta `[` o `]` en `if`, `else if`, `repeat` o `loop` |
| E030 | Declaracion de funcion mal formada | Falta el tipo de retorno, `:`, `compose` o el nombre de la funcion |
| E031 | Lista de parametros mal formada | Falta `(` o `)`, o un parametro sin tipo o sin nombre |
| E032 | Se esperaba una expresion | Operando ausente o token que no puede iniciar una expresion |
| E033 | Agrupacion sin cerrar | Falta `)` o `]` en una expresion, llamada, indexacion o literal de coleccion |
| E034 | Declaracion de variable o constante mal formada | Falta el identificador o el `=` en una declaracion |
| E035 | Sentencia de entrada/salida mal formada | Falta `>>` en `output`, o `<<` o el identificador en `input` |
| E036 | Cabecera de `loop` mal formada | La cabecera no sigue `[ declaracion; condicion; actualizacion ]` |
| E037 | Sentencia `mix` mal formada | Falta `{`, `}`, `:` tras `case`/`default`, o hay contenido fuera de un caso |
| E038 | Sentencia no valida | Token que no inicia ninguna sentencia, o expresion distinta de una llamada usada como sentencia |
| E039 | Elemento inesperado en el nivel superior | Contenido distinto de `import`, `tempo`, funcion o `session` fuera de un bloque |

#### 7.2.4 Errores de la innovacion (seccion 11)

| Codigo | Descripcion |
| --- | --- |
| E040 | Tempo fuera de rango (20 a 300 BPM) |
| E041 | Declaracion `tempo` no permitida (fuera del `.tmpr` o duplicada) |
| E042 | Intensidad de `pulse` no numerica |

### 7.3 Errores en tiempo de ejecucion

| Codigo | Descripcion |
| --- | --- |
| R001 | Division por cero |
| R002 | Indice fuera de limites |
| R003 | Desbordamiento aritmetico |
| R004 | Error de parseo en input |
| R005 | Llamada a funcion no definida |
| R006 | Lectura desde entrada estandar sin datos disponibles (EOF) |
| R007 | Linea vacia o invalida para input de tone |

### 7.4 Advertencias

| Codigo | Descripcion |
| --- | --- |
| W001 | Variable declarada pero no utilizada |
| W002 | Sombreado de identificador (shadowing) |
| W003 | Funcion declarada pero nunca invocada |
| W004 | Conversion implicita en concatenacion phrase |

---

## 8. EJEMPLOS COMPLETOS Y CORREGIDOS

### 8.1 Hola Mundo

```
session >
    output >> "Hola, Mundo";
<
```

### 8.2 Variables y operaciones

```
session >
    sample a = 10;
    sample b = 20;
    sample c = a + b;
    output >> "Resultado: " + c;
<
```

### 8.3 Condicionales

```
session >
    sample temp = 80;

    if [temp > 70] >
        output >> "Temperatura alta";
    >
    else >
        output >> "Temperatura normal";
    <
<
```

### 8.4 Bucles

```
session >
    # Loop (for)
    loop [sample i = 0; i < 5; i++] >
        output >> i;
    <

    # Repeat (while)
    sample contador = 0;
    repeat [contador < 5] >
        output >> contador;
        contador++;
    <
<
```

### 8.5 Funciones

```
sample : compose sumar(sample a, sample b) >
    ret a + b;
<

void : compose saludar(phrase nombre) >
    output >> "Hola, " + nombre;
    ret;
<

session >
    sample resultado = sumar(3, 4);
    output >> resultado;
    saludar("TM");
<
```

### 8.6 Secuencias y matrices

```
session >
    sequence<tone> notas = ['C', 'E', 'G'];
    output >> notas[0];
    notas[1] = 'D';

    score<sample> matriz = [
        [1, 2, 3],
        [4, 5, 6]
    ];
    output >> matriz[1][2];
<
```

### 8.7 Mix (switch)

```
session >
    sample opcion = 2;

    mix opcion >
        case 1: >
            output >> "Uno";
        <
        case 2: >
            output >> "Dos";
        <
        default: >
            output >> "Otro";
        <
    <
<
```

### 8.8 Constantes

```
session >
    const sample PI_ENTEROS = 3;
    const phrase SALUDO = "Hola";
    sample x = 10;
    # const sample Y = x + 5;  # ERROR: x no es constante
<
```

### 8.9 Programa integrado completo

```
# ============================================
# Archivo: matematicas.tm
# ============================================
sample : compose factorial(sample n) >
    if [n <= 1] >
        ret 1;
    >
    ret n * factorial(n - 1);
<

sample : compose sumar_lista(sequence<sample> nums) >
    sample total = 0;
    sample i = 0;
    repeat [i < nums.length()] >
        total = total + nums[i];
        i++;
    <
    ret total;
<
```

```
# ============================================
# Archivo: principal.tmpr
# ============================================
import "matematicas.tm"

session >
    sample num = 5;
    sample fact = factorial(num);
    output >> "Factorial de " + num + " es " + fact;

    sequence<sample> valores = [10, 20, 30, 40];
    sample suma = sumar_lista(valores);
    output >> "Suma de lista: " + suma;

    sample opcion = 2;
    mix opcion >
        case 1: >
            output >> "Opcion uno";
        <
        case 2: >
            output >> "Opcion dos";
        <
        default: >
            output >> "Otra opcion";
        <
    <

    sample contador = 0;
    repeat [contador < 3] >
        output >> "Contador: " + contador;
        contador++;
    <
<
```

---

## 9. TABLA DE PALABRAS RESERVADAS

| Palabra reservada | Proposito | Forma de uso | Ejemplo |
| --- | --- | --- | --- |
| `sample` | Tipo entero de 32 bits | Declaracion de variable entera | `sample edad = 25;` |
| `wave` | Tipo flotante de precision simple | Declaracion de variable flotante | `wave pi = 3.14;` |
| `tone` | Tipo caracter Unicode | Declaracion de variable caracter | `tone nota = 'C';` |
| `phrase` | Tipo cadena de texto | Declaracion de variable cadena | `phrase nombre = "Ana";` |
| `signal` | Tipo booleano | Declaracion de variable booleana | `signal activo = true;` |
| `silence` | Valor nulo | Asignacion a phrase | `phrase dato = silence;` |
| `sequence` | Tipo arreglo unidimensional | Declaracion de lista | `sequence<sample> nums = [1, 2, 3];` |
| `score` | Tipo matriz bidimensional | Declaracion de matriz | `score<sample> m = [[1,2],[3,4]];` |
| `void` | Tipo de retorno vacio | Funcion sin retorno | `void : compose saludar() > ... <` |
| `compose` | Definir funciones | Declaracion de funcion | `sample : compose sumar(sample a) > ... <` |
| `session` | Punto de entrada del programa | Bloque principal | `session > ... <` |
| `output` | Salida de datos a consola | Enviar expresion a salida | `output >> "Hola";` |
| `input` | Entrada de datos desde usuario | Leer valor en variable | `input << edad;` |
| `if` | Condicional simple | Evaluar condicion | `if [x > 0] > ... <` |
| `else` | Alternativa condicional | Bloque alternativo | `else > ... <` |
| `mix` | Condicional multiple (switch) | Seleccion por valor | `mix opcion > case 1: > ... < <` |
| `case` | Rama dentro de mix | Valor a comparar | `case 1: > ... <` |
| `default` | Rama por defecto en mix | Caso no contemplado | `default: > ... <` |
| `loop` | Ciclo for | Repeticion controlada | `loop [sample i=0; i<5; i++] > ... <` |
| `repeat` | Ciclo while | Repeticion condicional | `repeat [x < 10] > ... <` |
| `ret` | Retornar valor desde funcion | Devolver resultado | `ret a + b;` |
| `import` | Importar funciones de otro archivo | Inclusion de modulo | `import "utilidades.tm";` |
| `const` | Declarar variable inmutable | Constante | `const sample MAX = 100;` |
| `true` | Literal booleano verdadero | Valor signal | `signal ok = true;` |
| `false` | Literal booleano falso | Valor signal | `signal ok = false;` |
| `tempo` | Ritmo del proyecto en BPM | Declaracion en el `.tmpr` (seccion 11) | `tempo 120;` |
| `pulse` | Pulso del visualizador | Emitir una rafaga de particulas (seccion 11) | `pulse [nota * 10];` |

---

## 10. PROGRAMAS DE PRUEBA CANONICOS

Estos programas sirven como casos de referencia para validar el compilador
e interprete de TM.

### 10.1 Programas validos

**Prueba V1: Operaciones aritmeticas basicas**

```
session >
    sample a = 10;
    sample b = 3;
    sample suma = a + b;
    sample resta = a - b;
    sample mult = a * b;
    sample div = a / b;
    output >> "Suma: " + suma;
    output >> "Resta: " + resta;
    output >> "Mult: " + mult;
    output >> "Div: " + div;
<
```

*Resultado esperado:* Suma: 13, Resta: 7, Mult: 30, Div: 3

**Prueba V2: Condicionales anidados**

```
session >
    sample x = 15;
    if [x > 10] >
        if [x < 20] >
            output >> "Entre 10 y 20";
        <
    >
<
```

*Resultado esperado:* Entre 10 y 20

**Prueba V3: Recursion**

```
sample : compose fibonacci(sample n) >
    if [n <= 1] >
        ret n;
    >
    ret fibonacci(n - 1) + fibonacci(n - 2);
<

session >
    sample f = fibonacci(7);
    output >> "Fib(7) = " + f;
<
```

*Resultado esperado:* Fib(7) = 13

**Prueba V4: Manipulacion de secuencias**

```
session >
    sequence<sample> nums = [5, 10, 15, 20];
    sample i = 0;
    repeat [i < nums.length()] >
        nums[i] = nums[i] * 2;
        i++;
    <
    output >> nums[0];
    output >> nums[3];
<
```

*Resultado esperado:* 10, 40

**Prueba V5: Uso de constantes y tipos mixtos**

```
session >
    const sample LIMITE = 100;
    const phrase MENSAJE = "El limite es: ";
    output >> MENSAJE + LIMITE;
<
```

*Resultado esperado:* El limite es: 100

### 10.2 Errores sintacticos

**Prueba S1: Falta punto y coma**

```
session >
    sample x = 10  # Falta ;
<
```

*Error esperado:* E027 - Se esperaba `;` al final de la sentencia.

**Prueba S2: Delimitadores de bloque invertidos**

```
session <
    output >> 1;
>
```

*Error esperado:* E028 - Se esperaba `>` para iniciar el bloque.

**Prueba S3: Condicion sin corchetes**

```
session >
    if x > 0 >
        output >> 1;
    <
<
```

*Error esperado:* E029 - La condicion debe estar entre corchetes `[ ]`.

**Prueba S4: Funcion sin tipo de retorno**

```
compose funcion() >
    ret;
<
```

*Error esperado:* E030 - Se esperaba el tipo de retorno antes de `:`.

**Prueba S5: Matriz con filas de distinto tamano**

```
session >
    score<sample> m = [
        [1, 2],
        [3]
    ];
<
```

*Error esperado:* E019 - Todas las filas de una matriz deben tener el mismo tamano.

### 10.3 Errores semanticos

**Prueba M1: Tipo incompatible en asignacion**

```
session >
    sample x = 3.14;
<
```

*Error esperado:* E003 - No se puede asignar `wave` a `sample`.

**Prueba M2: Condicion no booleana**

```
session >
    sample x = 5;
    if [x + 1] >
        output >> 1;
    <
<
```

*Error esperado:* E005 - La condicion debe producir un valor de tipo `signal`.

**Prueba M3: Variable no declarada**

```
session >
    output >> y;
<
```

*Error esperado:* E001 - Identificador `y` no declarado.

**Prueba M4: Redeclaracion de funcion**

```
sample : compose sumar(sample a) > ret a; <
sample : compose sumar(sample b) > ret b; <
session > output >> sumar(1); <
```

*Error esperado:* E013 - Sobrecarga de funcion no permitida / E002 - Redeclaracion.

**Prueba M5: Modificacion de constante**

```
session >
    const sample X = 10;
    X = 20;
<
```

*Error esperado:* E009 - No se puede modificar una variable declarada como `const`.

### 10.4 Pruebas de importaciones

**Prueba I1: Importacion exitosa**
- Archivo `utilidades.tm` define `sample : compose doble(sample x)`.
- Archivo `principal.tmpr` importa `utilidades.tm` y llama `doble(5)`.
*Resultado esperado:* 10

**Prueba I2: Funcion importada no existe**
- Archivo `principal.tmpr` importa `utilidades.tm` pero llama `triple(5)`.
*Error esperado:* E001 - Funcion `triple` no declarada.

**Prueba I3: Tipos incompatibles en funcion importada**
- Archivo `utilidades.tm` define funcion con parametro `sample`.
- Archivo `principal.tmpr` la llama pasando un `phrase`.
*Error esperado:* E008 - Tipos de argumentos incompatibles.

### 10.5 Prueba de importacion circular

**Prueba C1: Ciclo A -> B -> A**
- `a.tm` importa `b.tm`.
- `b.tm` importa `a.tm`.
*Error esperado:* E012 - Importacion ciclica detectada entre `a.tm` y `b.tm`.

### 10.6 Prueba completa del problema del lenguaje

*(Esta prueba debe ser disenada por el equipo segun el problema especifico
que resuelva el lenguaje. Ejemplo: si TM se enfoca en procesamiento de notas
musicales, la prueba podria ser un programa que lea una secuencia de tonos,
valide que sean notas validas, y genere una matriz de acordes.)*

---

## 11. INNOVACION: VISUALIZADOR DE PARTICULAS

El IDE de TM incluye un panel que representa el estado del proyecto con una
animacion 3D de particulas dibujada con caracteres ASCII. El lenguaje controla
la animacion con dos construcciones propias del entorno de produccion musical:
`tempo`, que fija el ritmo, y `pulse`, que lanza rafagas de particulas desde el
programa en ejecucion.

### 11.1 Panel de particulas

- **Representacion:** las particulas son puntos en un espacio 3D proyectados
sobre una rejilla de caracteres monoespaciados. El caracter de cada celda
depende de la profundidad de la particula mas cercana, de lejos a cerca:
`" .:-=+*#%@"`.
- **Ritmo:** la animacion late al ritmo declarado con `tempo` (11.3). Cada pulso
(beat) produce una contraccion y expansion del conjunto de particulas.
- **Modos:**
    - *Edicion:* el panel muestra el estado armonico del proyecto (11.2).
    - *Ejecucion:* mientras se ejecuta el programa, cada sentencia `pulse`
    (11.4) agrega una rafaga sobre la animacion. Al terminar la ejecucion, el
    panel vuelve al modo edicion.

### 11.2 Estado armonico del proyecto

El estado se calcula con los diagnosticos de **todos los archivos del proyecto**
y se actualiza tras cada analisis (al editar y al compilar).

| Estado | Condicion | Comportamiento de las particulas |
| --- | --- | --- |
| Armonia | 0 errores y 0 advertencias | Orbita ordenada con pulso estable |
| Tension | 0 errores y 1 o mas advertencias | Vibracion y deriva irregular |
| Disonancia | 1 o mas errores | Turbulencia; la intensidad crece con el numero de errores y alcanza su maximo con 10 |

### 11.3 Declaracion `tempo`

**Sintaxis:**

```ebnf
tempo_decl = "tempo", integer_literal, ";" ;
```

**Semantica estatica:**
- Solo puede aparecer en el archivo `.tmpr`, despues de las importaciones y
antes de las funciones.
- Se permite como maximo una declaracion por proyecto. Una declaracion en un
`.tm`, o una segunda declaracion, produce E041.
- El valor debe estar entre 20 y 300 BPM, ambos incluidos. Fuera de ese rango
produce E040.
- Si el proyecto no declara `tempo`, se usa 120 BPM.

**Semantica dinamica:** fija la frecuencia de pulso del panel. No afecta a la
ejecucion del programa.

### 11.4 Sentencia `pulse`

**Sintaxis:**

```ebnf
pulse_stmt = "pulse", "[", expression, "]", ";" ;
```

**Semantica estatica:**
- Puede aparecer en cualquier bloque donde se admite una sentencia.
- La expresion debe ser de tipo `sample` o `wave`. Cualquier otro tipo produce
E042.

**Semantica dinamica:**
- Se evalua la expresion. Los errores de ejecucion de la expresion (por ejemplo,
R001) se reportan como en cualquier otra sentencia.
- La intensidad es el valor obtenido, limitado al rango [0, 100]. Con intensidad
0 no se emite ninguna rafaga; con intensidad mayor que 0 se emite una rafaga
cuyo tamano es proporcional a la intensidad.
- `pulse` no modifica variables ni escribe en la salida estandar. Fuera del IDE
(linea de comandos) evalua la expresion y no tiene efecto visible.

### 11.5 Ejemplos

**Programa valido:** un arpegio de tres notas por cada valor de la secuencia.

```
tempo 96;

void : compose acorde(sample raiz) >
    pulse [raiz];
    pulse [raiz + 4];
    pulse [raiz + 7];
    ret;
<

session >
    sequence<sample> notas = [40, 55, 70];
    loop [sample i = 0; i < notas.length(); i++] >
        acorde(notas[i]);
    <
<
```

**Errores:**

```
tempo 500;                  # E040: fuera del rango 20 a 300

session >
    pulse ["fuerte"];       # E042: la intensidad debe ser sample o wave
<
```

```
# utilidades.tm
tempo 120;                  # E041: tempo solo puede declararse en el .tmpr
```

### 11.6 Requisitos de la innovacion

- Debe tener utilidad practica para el programador.
- Debe estar integrada con el IDE (no solo en linea de comandos).
- Debe incluir validaciones sintacticas y semanticas.
- Debe funcionar durante la presentacion del proyecto.
- Debe documentarse con ejemplos de uso.

---

## 12. CONSIDERACIONES DE IMPLEMENTACION

### 12.1 Fases del compilador/interprete recomendadas

1. **Analisis lexico** (lexer): tokenizacion, manejo de comentarios.
2. **Analisis sintactico** (parser): construccion del AST segun la EBNF.
3. **Analisis semantico:** resolucion de nombres, verificacion de tipos,
validacion de constantes, deteccion de importaciones ciclicas.
4. **Generacion de codigo / Interpretacion:** ejecucion o traduccion a codigo
intermedio.

### 12.2 Decisiones de diseno a futuro

- **Gestion de memoria:** Se recomienda recoleccion de basura para `phrase`,
`sequence` y `score`.
- **Optimizacion:** Evaluacion de cortocircuito para `and` y `or`.
- **Biblioteca estandar:** Definir funciones para conversion de tipos
(`toSample`, `toWave`, etc.), operaciones de E/S avanzadas, y manipulacion
de secuencias.
- **Manejo de errores:** Implementar stack traces para errores en tiempo de
ejecucion.

---

## 13. HISTORIAL DE CAMBIOS RESPECTO AL DOCUMENTO ORIGINAL

| Elemento | Documento original | Especificacion formal v1.1 | Justificacion |
| --- | --- | --- | --- |
| Delimitadores de bloque | Inconsistentes (`> <` vs `< >`) | `>` inicio, `<` fin | Unificacion con EBNF original |
| Declaracion de variables | `:` y `=` usados indistintamente | Solo `=` | Consistencia sintactica |
| Funciones | Sintaxis inconsistente | `tipo : compose nombre(params)` | Unificacion |
| Parametros de funcion | `[]` en ejemplos | `()` en gramatica | `()` es estandar para parametros; `[]` para indexacion |
| Retorno | `ret valor;` vs `ret -> valor;` | `ret [expr];` | Simplicidad, consistencia con ejemplos |
| E/S | `output >>` / `input <<` | `output >>` / `input <<` | Corregida EBNF para coincidir con ejemplos |
| Tipos compuestos en EBNF | `score[T]` como tipo primitivo | `sequence<T>` y `score<T>` como composite_type | Claridad en jerarquia de tipos |
| `silence` vs `null` | Ambos usados | Solo `silence` | Unificacion terminologica |
| `track` | Aparecia en tabla | Eliminado | No definido en ningun contexto |
| `new` | En tabla de palabras reservadas | Eliminado | No utilizado en sintaxis actual |
| `from`/`to`/`step` | En tabla para `repeat` | Eliminados | No utilizados en sintaxis actual de `repeat` |
| Incremento `++` | En ejemplos, no en EBNF | Anadido a `update_expr` | Completacion de gramatica |
| `mix` | En palabras reservadas, no en EBNF | Anadido completo a gramatica | Completacion de gramatica |
| Comentarios | Solo mencionados informalmente | Formalizados en seccion lexica | Base para implementacion |
| Scope | No definido | Ambito lexico estatico | Base semantica indispensable |
| Paso de parametros | No definido | Por valor | Semantica clara y segura |
| Coercion | No definida | Fuerte, con excepcion de concatenacion phrase | Seguridad de tipos |
| Indentacion | No mencionada | Explicitamente NO significativa | Responde requisito del proyecto |
| Forward declaration | No definida | No necesaria (dos pasadas) | Facilidad de uso |
| Proyecto multiarchivo | No definido | Estructura con `.tm` y `.tmpr` | Requisito del proyecto |
| Visibilidad | No definida | Todo publico, sin namespaces | Simplicidad |
| Formato de errores | No definido | Formato estandarizado con codigos | Requisito del proyecto |
| Pruebas canonicas | No incluidas | 5 validos, 5 sintacticos, 5 semanticos, 3 importaciones, 1 circular | Requisito del proyecto |
| Innovacion | No incluida | 3 opciones propuestas con sintaxis y semantica | Requisito del proyecto |
| Tabla palabras reservadas | Lista simple | Tabla completa con proposito, uso y ejemplo | Requisito del proyecto |
| Secuencias de escape | Mencionadas brevemente | Especificadas con codigos Unicode | Precision tecnica |
| Operaciones compuestos | Solo indexacion | Asignacion por referencia, sin comparacion ni concatenacion | Semantica completa |
| `input` detallado | Descrito brevemente | Comportamiento por tipo, trim, EOF, errores | Precision para implementacion |

---

*Fin de la especificacion formal del lenguaje TM v1.1*
