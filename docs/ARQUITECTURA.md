# Arquitectura de tempo-rp

Estado real del repositorio. Lo que ya existe está descrito en presente; lo que
falta queda en "Hoja de ruta" al final para que no haya que re-derivarlo.

## Panorama

`tempo-rp` implementa el lenguaje **TM** (especificación en
[`ESPECIFICACION-TM.md`](ESPECIFICACION-TM.md)). Se compone de:

| Proyecto | TFM | Rol | Estado |
|---|---|---|---|
| `TmCompiler.Domain` | `net8.0` | Modelos puros del compilador, sin dependencias | Solo la capa léxica |
| `TmCompiler.Application` | `net8.0` | Casos de uso; depende solo de `Domain` | Solo el `Lexer` |
| `TmCompiler.Infrastructure` | `net8.0` | Servicios de presentación de diagnósticos; depende de `Domain` | Solo `LexerDiagnosticReporter` |
| `TmCompiler.Cli` | `net8.0` (Exe) | Arnés de consola del pipeline | Solo ejecuta el `Lexer` |
| `TmIde` | `net10.0-windows` (WPF) | IDE gráfico | Esqueleto visual (sin cablear al compilador) |
| `TmCompiler.Tests` | `net8.0` | Pruebas xUnit | Solo `LexerTests` |
| `TmInterpreter` | — | Carpeta vacía | Sin implementar |
| `TmSystemProject` | — | Carpeta vacía | Sin implementar |

La solución es [`tempo-rp.slnx`](../tempo-rp.slnx) (formato `.slnx`). No incluye a
`TmInterpreter` ni `TmSystemProject` porque todavía no tienen `.csproj`.

## `TmCompiler` — Clean Architecture por capas

La regla de dependencias se hace **físicamente verificable**: cada capa es su
propio proyecto, así que una capa interna no puede referenciar una externa aunque
se quisiera.

```
TmCompiler.Cli  ──►  TmCompiler.Application  ──►  TmCompiler.Domain
      │                                              ▲
      └──────────────►  TmCompiler.Infrastructure  ──┘
```

- **`Domain`** no referencia ningún otro proyecto.
- **`Application`** referencia solo a `Domain`.
- **`Infrastructure`** referencia solo a `Domain` (aún no necesita `Application`).
- **`Cli`** referencia a los tres.
- **`TmCompiler.Tests`** referencia a `Domain` y `Application`.

### `TmCompiler.Domain/Lexical/` — modelos léxicos

| Archivo | Qué es |
|---|---|
| `TokenType.cs` | `enum` exhaustivo de categorías de token de TM v1.1 (keywords, literales, operadores, puntuación, `EndOfFile`, `Error`). |
| `IToken.cs` | Contrato mínimo de un token: `TokenType`, `Lexeme`, `Line`, `Column` (todo `get`). Aplica ISP. |
| `Token.cs` | `readonly record struct Token(...)` que implementa `IToken`. Struct inmutable para minimizar asignaciones en heap al tokenizar fuentes grandes. `ToString()` da `[Tipo] 'lexema' (L, C)`. |
| `LexerException.cs` | `sealed` : `Exception` con `FileName`/`Line`/`Column`. `ToStandardFormat(code, description, suggestion)` produce el bloque de error `[ERROR E###]` de la §7 de la especificación. |

Las carpetas `Syntactic/` (AST) y `Semantic/` (tabla de símbolos) existen en la
estructura pero están vacías.

### `TmCompiler.Application/Lexing/` — el Lexer

`ILexer` expone un único método: `IEnumerable<Token> Tokenize()`. Aplica DIP: el
`Cli`, el futuro parser y el IDE dependen de la abstracción, no de la clase.

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

### `TmCompiler.Infrastructure/`

`LexerDiagnosticReporter` (estático): `GenerateTokenReport(tokens, fileName)`
devuelve una tabla de texto `Token Type | Lexeme | Location` para depuración. SRP:
separa generar tokens de presentarlos.

### `TmCompiler.Cli/Program.cs`

Top-level statements. `TmCompiler.Cli <archivo.tm>` → lee el archivo, corre el
`Lexer`, imprime el reporte de `LexerDiagnosticReporter`. Es un arnés temporal
para probar el pipeline sin `TmIde`; se ampliará con parser y análisis semántico
cuando existan.

## `TmIde` — IDE gráfico (WPF)

Proyecto **WPF SDK-style** sobre `net10.0-windows`. Hoy es **esqueleto visual**:
no referencia a `TmCompiler.*` todavía.

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
| [`Directory.Build.props`](../Directory.Build.props) | Props para **todos** los proyectos: `LangVersion=latest`, `Nullable=enable` (los de `TmCompiler` lo sobrescriben a `disable`), `ImplicitUsings=enable`, `AnalysisLevel=latest-Recommended`, `EnforceCodeStyleInBuild=true`, `TreatWarningsAsErrors=true`, `CodeAnalysisTreatWarningsAsErrors=false`. |
| [`Directory.Packages.props`](../Directory.Packages.props) | Central Package Management: `CommunityToolkit.Mvvm 8.4.2`; para tests `Microsoft.NET.Test.Sdk 17.11.1`, `xunit 2.9.2`, `xunit.runner.visualstudio 2.8.2`, `coverlet.collector 6.0.2`. Los `.csproj` referencian sin `Version`. |
| [`.editorconfig`](../.editorconfig) | Estilo + nomenclatura C#. Ver [`CONVENTIONS.md`](CONVENTIONS.md). |

### Comandos

```powershell
dotnet build                                   # compila todo; el estilo roto rompe el build
dotnet test                                    # corre TmCompiler.Tests (xUnit)
dotnet run --project src/TmCompiler/TmCompiler.Cli -- ruta/al/archivo.tm
```

`TmIde` requiere Windows (`net10.0-windows`, `UseWPF`).

## Hoja de ruta (no implementado)

- **Parser** de TM → AST en `TmCompiler.Domain/Syntactic/`, orquestado desde
  `TmCompiler.Application/Parsing/`. La especificación (EBNF sintáctico, §3) es la
  referencia autoritativa.
- **Análisis semántico**: tabla de símbolos y sistema de tipos en
  `TmCompiler.Domain/Semantic/` + `TmCompiler.Application/Analysis/`.
- **Cablear `TmIde` al compilador**: `ProjectReference` a `TmCompiler.*` y consumir
  `ILexer` (y luego el parser) desde los ViewModels; volcar diagnósticos al panel
  inferior.
- **Reemplazar el editor placeholder** por AvalonEdit con resaltado de sintaxis TM.
- **`TmInterpreter`** y **`TmSystemProject`**: se dividirán en las mismas capas
  (`Domain` / `Application` / `Infrastructure`) que `TmCompiler` cuando arranque su
  implementación.
- **Migrar `TmCompiler.*` a `<Nullable>enable</Nullable>`**.
