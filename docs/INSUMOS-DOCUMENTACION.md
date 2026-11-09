# Insumos para la documentación final (PDF)

Material de trabajo para redactar las secciones del PDF que exige el enunciado
(§17). Cada sección indica **qué pide el enunciado**, **qué datos ya existen en
el repositorio** y **qué debe aportar el equipo**. Los datos técnicos están
verificados contra `develop` / `feature/edicion-asistida` al 2026-11-07.

> Prioridad crítica: el enunciado exige que el lenguaje "responda a un problema
> previamente investigado" (§2, §3) y eso vale 12 % de la nota más la pregunta
> de defensa "Comprensión del problema y la solución". Hoy la especificación
> describe TM como "lenguaje de propósito general" (ESPECIFICACION-TM §1) y la
> prueba 10.6 "relacionada con el problema" está sin diseñar. Definir el
> problema es lo primero; todo lo demás de este documento se apoya en eso.

---

## 1. Datos duros del proyecto (para citar en cualquier sección)

| Dato | Valor |
| --- | --- |
| Nombre del lenguaje | Tempo (TM) |
| Extensiones | `.tmpr` (archivo principal, único por proyecto) y `.tm` (módulo) |
| Temática | Notación musical: `sample` (entero), `wave` (flotante), `tone` (carácter), `phrase` (cadena), `signal` (booleano), `silence` (nulo), `sequence<T>` (arreglo), `score<T>` (matriz), `compose` (función), `session` (punto de entrada), `mix` (switch), `loop`/`repeat` (for/while), `ret`, `tempo`, `pulse` |
| Palabras reservadas | 30 (tabla única en `src/Tempo.Language/Syntax/SyntaxFacts.cs`) |
| Bloques | Delimitados por `>` (inicio) y `<` (fin); la indentación **no** es significativa |
| Tipado | Estático y fuerte; única coerción implícita: concatenación con `phrase` (advierte W004) |
| Alcance | Léxico estático; ámbito por bloque; sin variables globales; funciones públicas y espacio de nombres único por proyecto |
| Paso de parámetros | Por valor; recursión permitida; sin sobrecarga; sin declaración adelantada (dos pasadas) |
| Diagnósticos | 42 errores de compilación (E001–E042), 7 de ejecución (R001–R007), 4 advertencias (W001–W004) |
| Formato de error | `[ERROR E003] Título` / `Archivo:` / `Línea:` / `Descripción:` / `Posible solución:` |
| Pipeline | Lexer → Parser (AST, 33 nodos) → Análisis semántico (ProjectAnalyzer, SymbolTable, reglas de tipos) → Intérprete árbol-recorrido |
| Tecnología | C# / .NET 10; biblioteca `Tempo.Language`, host de consola `Tempo.Cli`, IDE `TmIde` en WPF (MVVM con CommunityToolkit.Mvvm) |
| Pruebas automatizadas | 908 pruebas xUnit, todas pasan (619 del lenguaje, 289 del IDE) |
| Innovación | Visualizador de partículas 3D controlado por el lenguaje (`tempo`, `pulse`) y que refleja el "estado armónico" del proyecto (Armonía / Tensión / Disonancia según diagnósticos) |
| Repositorio | 76 commits en `develop`; autores: Arley Calderón Vargas (54), Juan Calderón Salazar (22) |

---

## 2. Portada

Aportar: universidad, sede, curso (Paradigmas de Programación), profesor (Pablo
A. Venegas Elizondo), título ("Tempo: …" + subtítulo que mencione el problema),
integrantes con carné, fecha de entrega, logotipo de Tempo (§5.1 lo exige: hoy
solo existen iconos en `src/Assets/`; confirmar o diseñar uno).

---

## 3. Introducción

Estructura sugerida (4 párrafos, 1 página):

1. **Contexto:** el problema elegido en una frase y por qué importa.
2. **Qué se construyó:** un lenguaje propio (Tempo), su compilador/intérprete y un
   IDE gráfico con proyectos multiarchivo.
3. **Enfoque:** temática musical como recurso para el problema; compilador por
   fases (modelo Roslyn/Minsk); errores en español con sugerencia de corrección.
4. **Organización del documento:** una línea por capítulo.

Datos que el equipo debe completar: el problema (ver §4) y la frase que une
"música" con ese problema. Sin esa conexión, la temática se lee como estética.

---

## 4. Planteamiento del problema (12 % + defensa)

El enunciado pide responder seis preguntas. Plantilla:

| Pregunta | Qué escribir | Evidencia que se espera |
| --- | --- | --- |
| ¿Cuál es el problema o motivación? | Enunciado concreto y acotado | Cita de un estudio, estadística o encuesta |
| ¿A quiénes afecta? | Perfil de usuario (edad, formación, contexto) | Datos de población o resultados de encuesta propia |
| ¿En qué contexto ocurre? | Dónde y cuándo (aula, curso introductorio, taller…) | Descripción del entorno real |
| ¿Qué soluciones existen? | 3–4 herramientas | Tabla comparativa (ver abajo) |
| ¿Qué limitaciones presentan? | Idioma, curva, mensajes de error, motivación… | Capturas o citas |
| ¿Cómo contribuye Tempo? | Mapear cada necesidad a una característica | Tabla necesidad → característica (ver §6) |

**Línea candidata coherente con lo que ya está construido** (debe validarse con
investigación real; no se puede presentar sin evidencia):

> Estudiantes que inician programación —en particular con afinidad por la música
> o el arte— abandonan o se frustran por la abstracción de los conceptos y por
> mensajes de error crípticos y en inglés. Tempo usa metáforas musicales para los
> tipos y estructuras, reporta errores en español con causa y sugerencia, y da
> retroalimentación visual inmediata (partículas) sobre la salud del programa.

Características ya implementadas que sostienen esa línea:

- Vocabulario musical consistente para tipos y estructuras.
- Errores en español con código, archivo, línea, descripción y "Posible
  solución" (incluidas sugerencias de ejecución en `RuntimeErrorSuggestions.cs`).
- Subrayado de errores mientras se escribe, autocompletado y fragmentos.
- Referencia del lenguaje integrada (F1) con ejemplos insertables.
- Visualización del estado del proyecto y de la ejecución (`tempo`/`pulse`).

**Evidencias de investigación** (sección propia del PDF): mínimo 3–5 fuentes
académicas o institucionales (tasas de reprobación/deserción en cursos
introductorios, estudios sobre mensajes de error, enseñanza mediante música o
creatividad) y, si es posible, una encuesta breve propia (10–20 respuestas) con
resultados graficados. Guardar respaldo en Anexos.

**Soluciones existentes** (tabla sugerida, completar con fuentes):

| Herramienta | Enfoque | Limitación frente al problema |
| --- | --- | --- |
| Scratch | Bloques visuales | No enseña sintaxis textual; difícil transición |
| PSeInt | Pseudocódigo en español | Sin proyectos multiarchivo reales; sin vínculo creativo |
| Sonic Pi | Música con código (Ruby) | Sintaxis y errores en inglés; enfocado en audio en vivo |
| Python / Java | Lenguajes generales | Mensajes técnicos en inglés; sin retroalimentación visual |

---

## 5. Objetivos

Redactar en infinitivo, medibles y alineados con lo construido.

**Objetivo general (propuesta):**
Diseñar e implementar Tempo, un lenguaje de programación con temática musical,
junto con su compilador, intérprete y entorno gráfico, para apoyar
[el problema definido en §4].

**Objetivos específicos (propuesta, cada uno verificable en el repositorio):**

1. Definir la especificación formal de Tempo (gramática EBNF, sistema de tipos,
   semántica estática y dinámica) → `docs/ESPECIFICACION-TM.md`.
2. Implementar un compilador por fases (léxico, sintáctico, semántico) que
   detecte y explique 42 tipos de error y 4 advertencias → `src/Tempo.Language`.
3. Implementar un intérprete que ejecute programas desde `session`, con
   entrada/salida y 7 errores de ejecución controlados → `Evaluation/`.
4. Soportar proyectos multiarchivo con importación de funciones y detección de
   ciclos → `Projects/ImportGraph.cs`, `ProjectAnalyzer.cs`.
5. Construir un IDE con árbol de archivos, editor con coloreado, autocompletado
   y búsqueda, panel de salida y botones de compilar/ejecutar → `src/TmIde`.
6. Implementar la innovación (visualizador de partículas guiado por `tempo` y
   `pulse`) integrada con el análisis y la ejecución.
7. Validar el sistema con pruebas automatizadas y los casos mínimos del
   enunciado (§18).

---

## 6. Propuesta de solución

Puntos que pide el enunciado (§4) y de dónde sacar la información:

| Punto | Fuente / contenido |
| --- | --- |
| Usuarios | Los definidos en §4 |
| Contexto de uso | Aula, práctica individual; Windows con .NET 10 |
| Alcance | Ver §7 de este documento |
| Limitaciones | Ver §7 |
| Beneficios esperados | Derivarlos de la tabla necesidad → característica |
| Características principales | Tabla de §1 |
| Ejemplos de programas | `samples/`, ESPECIFICACION §8 y §11.5, más el programa del problema (§10.6, pendiente) |
| ¿Por qué un lenguaje nuevo? | Control total del vocabulario, de los mensajes de error y de la integración visual; imposible en un lenguaje existente sin perder coherencia |

Tabla clave para la rúbrica ("Relación entre problema y lenguaje"):

| Necesidad detectada | Característica de Tempo |
| --- | --- |
| Conceptos abstractos | Metáforas musicales en tipos y estructuras |
| Errores incomprensibles | Formato estandarizado en español con posible solución |
| Falta de retroalimentación inmediata | Diagnósticos al escribir + estado armónico visual |
| Desorden al crecer el programa | Proyectos `.tmpr` + módulos `.tm` con `import` |
| Curva de entrada | Referencia integrada con ejemplos insertables, autocompletado |

---

## 7. Alcance y limitaciones

**Alcance (implementado y probado):**

- Tipos simples: `sample`, `wave`, `tone`, `phrase`, `signal`, `silence`.
- Compuestos: `sequence<T>` (arreglo) y `score<T>` (matriz), tamaño fijo.
- Condicional simple, con alternativa, múltiple (`else if` y `mix`).
- Ciclos `loop` (for) y `repeat` (while).
- Funciones con y sin retorno, parámetros, recursión; punto de entrada `session`.
- Importaciones entre archivos con validación de existencia, función,
  aridad, tipos, duplicados, ciclos y errores en archivos importados.
- IDE: crear/abrir proyecto, crear/renombrar/eliminar archivos y carpetas,
  pestañas, guardar, compilar, ejecutar, detener, entrada interactiva,
  temas, búsqueda/reemplazo, autocompletado, números de línea, coloreado.

**Limitaciones (declararlas evita preguntas incómodas en defensa):**

- Intérprete (no genera código máquina ni intermedio).
- Sin biblioteca estándar: no hay conversiones explícitas (`toSample`, etc.).
- Colecciones de tamaño fijo; asignación por referencia; sin diccionarios.
- Importación textual, sin espacios de nombres; todas las funciones públicas.
- Sin variables globales.
- Sin depurador paso a paso ni breakpoints (se retiró el panel de depuración).
- IDE solo para Windows (WPF); el CLI corre donde haya .NET.
- `pulse` no tiene efecto fuera del IDE.

---

## 8. Secciones técnicas: fuente ya escrita

Estas secciones pueden redactarse casi directamente desde la especificación:

| Sección del PDF | Fuente |
| --- | --- |
| Descripción del lenguaje / Identidad | ESPECIFICACION §1 + tabla §1 de este documento (falta logotipo y "filosofía de diseño": proponer 3 principios, p. ej. *claridad visual*, *errores que enseñan*, *estructura explícita*) |
| Palabras reservadas | ESPECIFICACION §9 (ya tiene propósito, uso y ejemplo) |
| Sintaxis | ESPECIFICACION §3 (EBNF) y §8 |
| Semántica | ESPECIFICACION §5 y §6 (responde los 11 puntos de §5.4 del enunciado) |
| Tipos de datos | ESPECIFICACION §4 (declaración, uso, operaciones, restricciones) |
| Funciones / Estructuras de control / Operaciones | ESPECIFICACION §5.3, §5.6, §4.4.2 |
| Organización de archivos / Importaciones | ESPECIFICACION §6.5, §6.6 |
| Manejo de errores | ESPECIFICACION §7 |
| Innovación | ESPECIFICACION §11 + explicación técnica (FibonacciSphere, IRuntimePulse, cálculo del estado armónico) |
| Arquitectura | `docs/ARQUITECTURA.md` (actualizado: fases del compilador, composición del IDE, flujo de compilar/ejecutar, innovación y pruebas) |
| Proceso de compilación | `Compilation.cs` → `TempoProject` → `ImportGraph` → `ProjectAnalyzer` → `SemanticAnalyzer`; diagrama de fases |
| Proceso de ejecución | `Interpreter` → `StatementExecutor` / `ExpressionEvaluator` / `FunctionInvoker`, pila con `RuntimeCallTracker`, `IRuntimeInput/Output/Pulse` |

---

## 9. Pruebas (§18 del enunciado)

Los casos canónicos están en ESPECIFICACION §10 y cubiertos por pruebas xUnit.
Para el PDF cada prueba necesita: **objetivo, entrada, resultado esperado,
resultado obtenido (captura del IDE), estado**.

| Exigido | Disponible | Pendiente |
| --- | --- | --- |
| 5 programas válidos | V1–V5 | Capturas |
| 5 errores sintácticos | S1–S5 (E027, E028, E029, E030, E019) | Capturas. Nota: S5 (E019) es semántico en el catálogo; considerar sustituirlo por E033 o E036 |
| 5 errores semánticos | M1–M5 | Capturas |
| 3 importaciones | I1, I2, I3 (`samples/importaciones`, `samples/prueba-r001-importado`) | Crear carpeta de muestra para I2 e I3 si no existe; capturas |
| 1 importación circular | C1 | Crear `samples/` con `a.tm` ↔ `b.tm`; captura |
| 1 prueba completa del problema | **No existe** | Diseñarla según §4 |

Agregar como cifra global: 908 pruebas automatizadas, 0 fallos.

---

## 10. Contribuciones, conclusiones, referencias, anexos

- **Contribuciones:** extraer de `git shortlog -sn develop` y de los PR; describir
  qué módulo hizo cada integrante. Hoy solo aparecen dos autores en el historial;
  si el equipo tiene un tercer integrante, el enunciado exige que tenga
  contribuciones visibles (§16, §20).
- **Conclusiones:** una por objetivo específico (logrado / parcial), más
  lecciones sobre paradigmas (imperativo, tipado estático, ámbito léxico).
- **Referencias:** formato APA; incluir las fuentes del problema, Minsk
  (Immo Landwerth), documentación de .NET/WPF, CommunityToolkit.Mvvm.
- **Anexos:** encuesta, gramática EBNF completa, catálogo de errores, manual de
  usuario (entregable propio de §19).

---

## 11. Preguntas que el equipo debe responder antes de redactar

1. ¿Cuál es el problema exacto y quiénes son los usuarios?
2. ¿Qué fuentes respaldan la existencia del problema?
3. ¿Por qué la temática musical ayuda a ese problema (y no solo decora)?
4. ¿Qué programa demuestra que Tempo resuelve parte del problema (prueba 10.6)?
5. ¿Cuál es el logotipo oficial?
6. ¿Cuántos integrantes y qué aportó cada uno?
