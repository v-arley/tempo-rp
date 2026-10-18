# Arquitectura de tempo-rp

Describe el estado del repositorio. El trabajo no implementado se recoge en
"Hoja de ruta", al final.

## Panorama

`tempo-rp` implementa el lenguaje **TM** (especificación en
[`ESPECIFICACION-TM.md`](ESPECIFICACION-TM.md)). Se compone de:

| Proyecto | TFM | Rol | Estado |
|---|---|---|---|
| `src/Tempo.Language` | `net8.0` | Biblioteca con todo el lenguaje: léxico, sintaxis, semántica e intérprete | Pipeline completo de un archivo |
| `src/Tempo.Cli` | `net8.0` (Exe) | Host de consola: ejecuta el pipeline y aporta la E/S por consola | Funcional |
| `src/TmIde` | `net10.0-windows` (WPF) | IDE gráfico | Esqueleto visual (sin cablear al lenguaje) |
| `tests/Tempo.Language.Tests` | `net8.0` | Pruebas xUnit de todas las fases | 479 tests |

La solución es [`tempo-rp.slnx`](../tempo-rp.slnx) (formato `.slnx`). Los
programas de ejemplo están en [`samples/`](../samples).

## `Tempo.Language` — modelo Roslyn/Minsk

La biblioteca sigue el enfoque de Roslyn y de Minsk, "el compilador como
API": **un solo proyecto**, organizado en carpetas según la **fase del
pipeline**. Cada carpeta es un namespace (`Tempo.Language.<Carpeta>`).

```
                Compilation  (fachada: un proyecto entra, diagnósticos y ejecución salen)
                   │
directorio ──► Projects (TempoProject → ImportGraph → ProjectRules)
o archivo         │
                   ▼
                Syntax (SyntaxTree por archivo: Lexer → tokens → Parser → AST)
                   │
                   ▼
                Binding (ProjectAnalyzer → SemanticAnalyzer por archivo)  ◄── Symbols (tipos y símbolos)
                   │                                                      ◄── Diagnostics (formato §7)
                   ▼
                Evaluation (Interpreter ejecuta el AST de entrada con sus importaciones)
```

```
Tempo.Cli ──► Tempo.Language          Tempo.Language.Tests ──► Tempo.Language
```

| Carpeta | Contenido |
|---|---|
| raíz | `Compilation`: fachada del compilador. `Compilation.FromPath(ruta)` analiza un proyecto completo; `IsExecutable` y `Run(entrada, salida)` lo ejecutan. |
| `Projects/` | `TempoProject` (se abre desde un directorio, con las reglas de §6.6, o desde un archivo, que se completa con lo que importa); `ImportGraph` (resuelve `import`: E014, detecta ciclos: E012, y da el orden de dependencias); reglas estructurales con el patrón Specification: `IProjectRule`, `ProjectRules.All`, `ExactlyOneProjectFileRule` (E016/E017) y `ModulesDeclareNoSessionRule` (E015). |
| `Syntax/` | `SyntaxTree` (un archivo analizado: AST y errores léxicos o sintácticos como `Diagnostic`), `ILexer`/`Lexer`, `IToken`/`Token`/`TokenType`, `LexerException`, `IParser`/`Parser`, `ParserException` y el AST: 33 `record` (bases `AstNode`, `ExpressionNode`, `StatementNode`). |
| `Symbols/` | Modelo de tipos y símbolos: `TmType` (record con igualdad estructural), `TypeKind`, `TypeNameParser` (único punto que interpreta nombres como `score<sample>`), `FunctionSymbol`, `VariableSymbol`, `SymbolKind`. |
| `Binding/` | Análisis semántico (§5): `ProjectAnalyzer` (analiza cada archivo en orden de dependencias con las firmas que puede ver; reporta E013 entre archivos y W003 sobre todo el proyecto), `SemanticAnalyzer` (un archivo; E020 para llamadas a funciones de módulos no importados), `FunctionSignatureCollector` (primera pasada), `StatementAnalyzer`, `ExpressionTypeResolver`, `ReturnPathAnalyzer`, `ConstantExpressionEvaluator`; `SymbolTable`/`Scope`, `DeclarationResult`/`DeclarationStatus`; reglas de tipos `OperatorRuleTable`/`OperatorSignature`, `TypeCompatibility`. |
| `Diagnostics/` | `Diagnostic` (bloque de cinco campos de la §7.1), `DiagnosticBag`, `ProjectDiagnostics` (diagnósticos de todos los archivos, ordenados por archivo y línea), `DiagnosticSeverity`, `DiagnosticCodes` y `DiagnosticCatalog` (títulos de las tablas §7.2 y §7.4). |
| `Evaluation/` | Intérprete: `Interpreter`, `StatementExecutor`, `ExpressionEvaluator`, `FunctionInvoker`/`FunctionRegistry`, `ReturnSignal`; modelo de ejecución `RuntimeValue`/`RuntimeValueKind`, `RuntimeSequence`, `RuntimeScore`, `RuntimeEnvironment`, `RuntimeVariable`, `RuntimeCallFrame`/`RuntimeCallTracker`; `TmRuntimeException` con `RuntimeErrorCodes` (`R001`–`R007`); puertos de E/S `IRuntimeInput`/`IRuntimeOutput`. |
| `IO/` | Reportes de texto para hosts: `LexerDiagnosticReporter` (tabla de tokens), `AstDiagnosticReporter` (árbol), `SemanticDiagnosticReporter` (delega en `Diagnostic.ToStandardFormat`). |

Reglas de diseño:

- **La biblioteca no conoce a ningún host.** La E/S del programa TM entra por
  `IRuntimeInput`/`IRuntimeOutput`. Las implementaciones de consola viven en
  `Tempo.Cli`.
- **Errores según la fase.** El lexer y el parser **lanzan** (`LexerException`,
  `ParserException`) al primer error, y `SyntaxTree` los convierte en un
  `Diagnostic` del archivo, de modo que un archivo con errores no detiene el
  análisis de los demás. El análisis semántico **no lanza**: acumula los
  `Diagnostic` y continúa, así una ejecución reporta todos los problemas. El
  intérprete lanza `TmRuntimeException` con un código `R###`.
- **Importación textual (§6.5).** El programa que ejecuta `Compilation.Run` es
  el AST del archivo de entrada con las funciones de todos los archivos que
  importa, directa o indirectamente (`ProgramNode with { Functions = … }`). El
  intérprete no distingue entre un proyecto de un archivo y uno de varios.
- **Nullable.** El proyecto usa `<Nullable>annotations</Nullable>`. Admite las
  anotaciones `?` de `Evaluation/` y el resto de carpetas, que no las declaran,
  sin emitir advertencias de nulabilidad.

### `Syntax/` — modelos léxicos

| Archivo | Qué es |
|---|---|
| `TokenType.cs` | `enum` exhaustivo de categorías de token de TM v1.1 (keywords, literales, operadores, puntuación, `EndOfFile`, `Error`). |
| `IToken.cs` | Contrato mínimo de un token: `TokenType`, `Lexeme`, `Line`, `Column` (todo `get`). Aplica ISP. |
| `Token.cs` | `readonly record struct Token(...)` que implementa `IToken`. Struct inmutable para minimizar asignaciones en heap al tokenizar fuentes grandes. `ToString()` da `[Tipo] 'lexema' (L, C)`. |
| `LexerException.cs` | `sealed` : `Exception` con `FileName`/`Line`/`Column`. `ToStandardFormat(code, description, suggestion)` produce el bloque de error `[ERROR E###]` de la §7 de la especificación. |

### `Syntax/` — el Lexer

`ILexer` expone un único método: `IEnumerable<Token> Tokenize()`. Aplica DIP: el
`Parser`, `Tempo.Cli` y el futuro IDE dependen de la abstracción, no de la clase.

`Lexer` es `sealed`. Diseño:

- **Estado encapsulado**: `_source`, `_fileName`, `_length`, y los cursores
  `_position` (0-based), `_line` / `_column` (1-based). Nada de esto se expone.
- **Palabras reservadas**: `Dictionary<string, TokenType>` estático poblado una
  sola vez en el constructor estático, con `StringComparer.Ordinal` (TM es
  sensible a mayúsculas/minúsculas). Lookup O(1). 27 keywords:
  `compose session output input mix loop repeat if else ret import const sequence
  score true false silence sample wave tone phrase signal void and or not`.
- **Pipeline** (`Tokenize`): bucle `SkipWhitespaceAndComments()` → `ReadNextToken()`
  hasta EOF, y siempre agrega un token `EndOfFile` final.
- **Sin estado de error intermedio**: `ReadNextToken()` nunca devuelve
  `TokenType.Error`; todo error léxico lanza `LexerException`. `Tokenize()` o
  completa el recorrido o propaga la excepción.
- **Primitivas**: `Peek()` / `PeekNext()` (lookahead de 2 para `>=`, `<=`, `==`,
  `!=`, `>>`, `<<`, `++`, `--`), `Advance()` (avanza y lleva la cuenta de
  línea/columna al cruzar `\n`).
- **Reconocedores**: `ReadIdentifierOrKeyword`, `ReadNumericLiteral` (entero vs.
  flotante; el `-` **no** forma parte del literal, se emite como `Minus` suelto),
  `ReadStringLiteral` (escapes `\n \t \" \\`; prohíbe salto de línea sin cerrar),
  `ReadCharLiteral` (exactamente un carácter o escape), `ReadOperatorOrPunctuation`
  (switch con lookahead).
- **`>` y `<` sueltos** se emiten siempre como `GreaterThan` / `LessThan` a nivel
  léxico. Que actúen como delimitadores de bloque (§3.2.1 de la especificación) es
  una decisión **del parser** según la posición sintáctica, no del lexer.
- **Comentarios**: línea `# ...` y bloque `## ... ##` (no anidable; EOF sin cerrar
  el bloque → `LexerException`).

## `Tempo.Cli` — host de consola

Contiene `Program.cs` (top-level statements) y los adaptadores de consola
`ConsoleRuntimeInput` y `ConsoleRuntimeOutput`. Se invoca con
`<directorio|archivo.tmpr|archivo.tm> [--tokens]`:

- Con un **directorio**, compila todos sus `.tm` y `.tmpr` como un proyecto
  (§6.6).
- Con un **archivo**, compila ese archivo y los que importa.

Pasos:

1. Compila con `Compilation.FromPath`.
2. Con `--tokens`, imprime la tabla de tokens del archivo de entrada
   (`LexerDiagnosticReporter`), y después su árbol (`AstDiagnosticReporter`).
3. Imprime un reporte de diagnósticos por archivo con
   `SemanticDiagnosticReporter`: siempre el del archivo de entrada, y los demás
   solo si tienen diagnósticos. Si hay errores, termina con código de salida 1.
4. Si la entrada es un `.tmpr` con `session` (los `.tm` son módulos sin punto
   de entrada), ejecuta el proyecto con `Compilation.Run` sobre la E/S de
   consola.

Los errores léxicos y sintácticos utilizan códigos específicos
del E021 al E039, definidos en DiagnosticCodes y registrados
en DiagnosticCatalog. Los errores de ejecución se imprimen 
como `R### (línea N): mensaje`, con código de salida 1.

## `Tempo.Language.Tests`

Un único proyecto xUnit. Sus carpetas reflejan las de la biblioteca: `Syntax/`,
`Symbols/`, `Binding/` (incluye `SemanticTestHarness`), `Diagnostics/`,
`Evaluation/` (incluye los programas canónicos de la especificación) y
`Projects/` (pruebas canónicas I1–I3 y C1, y reglas de proyecto). Los
namespaces siguen el patrón `Tempo.Language.Tests.<Carpeta>`.

`ProjectWorkspace` escribe un proyecto en un directorio temporal a partir de sus
archivos, lo compila contra el disco real y lo borra al terminar:
`ProjectWorkspace.With(("principal.tmpr", "..."), ("utilidades.tm", "..."))`.

## `TmIde` — IDE gráfico (WPF)

Proyecto **WPF SDK-style** sobre `net10.0-windows`. Es un **esqueleto visual**:
no referencia a `Tempo.Language`.

- **MVVM con `CommunityToolkit.Mvvm`** (`[ObservableProperty]` / `[RelayCommand]`).
  `ViewModelBase : ObservableObject`; los ViewModels que generan comandos son
  `sealed partial`.
- **Composición manual en `App.OnStartup`**, sin contenedor de DI: se crea
  `ThemeService`, luego `MainWindowViewModel`, luego `MainWindow` con ese VM como
  `DataContext`.
- **`MainWindowViewModel`** es el VM raíz: agrega los VM de cada región del shell
  — `EditorHostViewModel`, `BottomPanelViewModel`, `FileExplorerViewModel`,
  `MenuBarViewModel`, `StatusBarViewModel` — y expone `ToggleThemeCommand`. Los
  VM de región no dependen entre sí (`StatusBarViewModel` solo observa a
  `EditorHostViewModel` para el `LN/COL`).
- **Estado inicial vacío**: el árbol del explorador (`Roots`), las pestañas del
  editor (`Documents`) y las líneas del panel Terminal arrancan sin contenido; no
  hay datos de ejemplo.
- **Controles decorativos, sin acción**: los botones de crear archivo/carpeta de
  la cabecera del explorador y los `COMPILE` / `RUN` / `DEBUG` de la barra de
  estado no tienen `Command` — se mantienen solo por su forma. Por eso
  `FileExplorerViewModel` no recibe ningún callback y `StatusBarViewModel` solo
  recibe el `EditorHostViewModel`.
- **Ventana sin chrome nativo**: `WindowStyle=None` + `System.Windows.Shell.WindowChrome`.
  El arrastre y el doble clic para maximizar/restaurar están en el code-behind de
  `MainWindow`; los botones min/max/close en `TitleMenuBarView`.
- **Tema claro/oscuro en caliente** (`IThemeService` / `ThemeService`): intercambia
  el `ResourceDictionary` de tema en `Application.Current.Resources.MergedDictionaries`
  (añade el nuevo antes de quitar el viejo para no dejar un frame sin brushes).
  `Resources/Theme.Light.xaml` y `Theme.Dark.xaml` declaran las **mismas 15 claves**;
  las vistas las consumen con `{DynamicResource}` y reaccionan al swap sin reiniciar.
  Fondos por rol: `WindowBackgroundBrush` (marco + título), `SurfaceBackgroundBrush`
  (editor / terminal / barra de estado), `PanelOverlayBrush` (cuerpo del explorador),
  `PanelHeaderBrush` (cabecera del explorador), `InputBackgroundBrush` (campos de
  texto). Bordes: `DividerBrush` (líneas que enmarcan ventana y paneles),
  `InputBorderBrush` (borde en reposo de los campos). El resto: `HoverBackgroundBrush`,
  `ScrollbarThumbBrush`, `AccentBrush` y `CloseButtonHoverBrush` (idénticos en ambos
  temas), y los tres foregrounds + `TitleMenuForegroundBrush`. La paleta teal de los
  botones de la barra de estado es fija (no sigue el tema).
- **Editor**: `LineNumberedTextBox` — `TextBox` monoespaciado con canaleta de
  números de línea sincronizada por scroll. Sin resaltado de sintaxis ni plegado.
- **Estructura de carpetas**: `Views/` (+ subcarpetas `Editor/`, `Explorer/`,
  `Panel/`, `Shell/`), `ViewModels/` (mismas subcarpetas), `Services/`,
  `Controls/`, `Converters/`, `Design/` (datos de diseño), `Resources/`
  (`Icons.xaml`, `Styles.xaml`, los dos temas).
- **Fuentes**: `src/Assets/JetBrainsMono-Medium.ttf` e `IBMPlexMono-Medium.ttf`
  (origen único compartido) se embeben como `Resource` con `Link` en el `.csproj`.

## Infraestructura de build (raíz del repo)

| Archivo | Función |
|---|---|
| [`global.json`](../global.json) | Fija el SDK de .NET en `10.0.400`, `rollForward: latestFeature`, sin prerelease. |
| [`Directory.Build.props`](../Directory.Build.props) | Props para **todos** los proyectos: `LangVersion=latest`, `Nullable=enable` (`Tempo.Language`, `Tempo.Cli` y `Tempo.Language.Tests` lo sobrescriben a `annotations`), `ImplicitUsings=enable`, `AnalysisLevel=latest-Recommended`, `EnforceCodeStyleInBuild=true`, `TreatWarningsAsErrors=true`, `CodeAnalysisTreatWarningsAsErrors=false`. |
| [`Directory.Packages.props`](../Directory.Packages.props) | Central Package Management: `CommunityToolkit.Mvvm 8.4.2`; para tests `Microsoft.NET.Test.Sdk 17.11.1`, `xunit 2.9.2`, `xunit.runner.visualstudio 2.8.2`, `coverlet.collector 6.0.2`. Los `.csproj` referencian sin `Version`. |
| [`.editorconfig`](../.editorconfig) | Estilo + nomenclatura C#. Ver [`CONVENTIONS.md`](CONVENTIONS.md). |

### Comandos

```powershell
dotnet build                                   # compila todo; el estilo roto rompe el build
dotnet test                                    # corre Tempo.Language.Tests (xUnit)
dotnet run --project src/Tempo.Cli -- samples/prueba-correcta.tmpr [--tokens]
dotnet run --project src/Tempo.Cli -- samples/importaciones          # proyecto multiarchivo
```

`TmIde` requiere Windows (`net10.0-windows`, `UseWPF`).

## Hoja de ruta (no implementado)

- **Cablear `TmIde` al lenguaje**: añadir un `ProjectReference` a
  `Tempo.Language`, consumir el pipeline desde los ViewModels y volcar los
  diagnósticos al panel inferior.
- **Editor con AvalonEdit** y resaltado de sintaxis TM en `TmIde`.
- **`<Nullable>enable</Nullable>` en `Tempo.Language`**, con anotaciones de
  nulabilidad en todas sus carpetas.
