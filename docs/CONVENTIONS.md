# Convenciones de código — tempo-rp

Estándar de nomenclatura de C#/.NET (Microsoft), sin notación húngara. Se aplica y
verifica **en compilación** vía [`.editorconfig`](../.editorconfig) +
[`Directory.Build.props`](../Directory.Build.props):

- `EnforceCodeStyleInBuild` está activo para toda la solución, así que las reglas
  de estilo de `.editorconfig` se emiten como *warnings* al hacer `dotnet build`.
- `TreatWarningsAsErrors` es `true`, así que esos *warnings* de estilo **rompen el
  build**.
- `CodeAnalysisTreatWarningsAsErrors` es `false`: los diagnósticos de los
  analizadores (`CAxxxx`) quedan solo como advertencia y no bloquean el build.

No hace falta un linter aparte: `dotnet build` es la verificación.

## Regla general

**El nombre describe qué es o qué hace, no de qué tipo es.** El compilador y el IDE
ya conocen el tipo de cada símbolo; un prefijo como `iFlag`, `sTitle` o `fnPlay` no
aporta información y además rompe la generación de código de `CommunityToolkit.Mvvm`
que usa `TmIde` (ver más abajo).

## Tabla de nomenclatura

Cada fila corresponde a una `dotnet_naming_rule` de [`.editorconfig`](../.editorconfig).

| Elemento | Estilo | Ejemplo (del repo) |
|---|---|---|
| Namespace, clase, struct, enum, delegate | `PascalCase` | `Tempo.Language.Syntax`, `Lexer`, `TokenType` |
| Interfaz | `PascalCase` con prefijo `I` | `ILexer`, `IToken`, `IThemeService` |
| Método, propiedad, evento (público o no-privado) | `PascalCase` | `Tokenize()`, `Current`, `Toggle()` |
| Campo constante (`const`) | `PascalCase` | `ThemeDictionaryMarker` en `ThemeService` |
| Campo privado | `_camelCase` | `_source`, `_position`, `_themeService` |
| Campo privado estático | `_camelCase` | `_hostWindow` en `TitleMenuBarView` |
| Parámetro, variable local | `camelCase` | `source`, `fileName`, `startLine` |
| Parámetro de tipo genérico | `PascalCase` con prefijo `T` | `TResult` |
| Método asíncrono | `PascalCase` + sufijo `Async` | (no hay ninguno en el repo) |

> **Excepción vigente en el código:** las tablas de búsqueda `private static
> readonly` que actúan como constante lógica se nombran en `PascalCase`, igual que
> un `const`. Caso real: `Lexer.ReservedWords` (el diccionario de palabras
> reservadas de TM). Es una desviación consciente de la regla
> `static_private_fields`; cualquier otro campo `private static` sí usa `_camelCase`.

## Convenciones propias del proyecto (consistentes con el estándar de MS)

- **Manejadores de evento en code-behind de vistas WPF**: prefijo `On` + descripción
  del origen y el evento. Casos reales: `OnStateChanged`,
  `OnTitleBarMouseLeftButtonDown` (`MainWindow`), `OnMinimize`,
  `OnMaximizeRestore`, `OnClose` (`TitleMenuBarView`). Es el patrón de las
  plantillas de WPF.
- **Prefijo `On` también para manejadores privados de `PropertyChanged`**:
  `OnEditorHostPropertyChanged` en `MainWindowViewModel`.
- **Propiedades y campos booleanos**: prefijo semántico `Is`/`Has`/`Can` cuando el
  nombre no es evidente por sí solo (`IsAtEnd()` en `Lexer`). No es obligatorio
  para nombres ya inequívocos.
- **Un tipo público por archivo**, y el nombre de archivo coincide exactamente con
  el tipo (`Lexer.cs` → `sealed class Lexer`; `MainWindowViewModel.cs` →
  `sealed partial class MainWindowViewModel`).
- **`namespace` con ámbito de archivo** (`namespace Tempo.Language.Syntax;`),
  no con bloque `{ }`. Lo exige `csharp_style_namespace_declarations = file_scoped`.
- **El namespace coincide con la carpeta** (modelo Roslyn/Minsk):
  `src/Tempo.Language/Binding/` → `Tempo.Language.Binding`;
  `tests/Tempo.Language.Tests/Syntax/` → `Tempo.Language.Tests.Syntax`.
- **Llaves en línea propia** (estilo Allman), y **siempre llaves** aunque el cuerpo
  sea de una línea (`csharp_prefer_braces = true`).
- **Sin `this.`** para cualificar campos/propiedades/métodos/eventos
  (`dotnet_style_qualification_for_* = false`).
- **`var`** para tipos integrados y cuando el tipo es evidente (solo `suggestion`,
  no rompe el build).
- **Métodos con cuerpo de expresión** permitidos solo en propiedades
  (`expression_bodied_properties = true`), no en métodos (`= false`).

## Nullable

- `TmIde` (WPF, `net10.0-windows`) usa `<Nullable>enable</Nullable>`, el valor
  de `Directory.Build.props`.
- `Tempo.Language`, `Tempo.Cli` y `Tempo.Language.Tests` (`net8.0`) declaran
  `<Nullable>annotations</Nullable>`. El código de `Evaluation/` usa anotaciones
  `?` y el del resto de carpetas no las declara. El contexto `annotations`
  admite ambos estilos y no emite advertencias de nulabilidad.

## Patrones de las librerías usadas (no son "excepciones", es cómo funcionan)

### CommunityToolkit.Mvvm — `[ObservableProperty]` (solo en `TmIde`)

Un campo privado en `_camelCase` genera automáticamente la propiedad pública en
`PascalCase`. El nombre del campo **debe** seguir el estándar para que el generador
funcione. Caso real en `MainWindowViewModel`:

```csharp
[ObservableProperty]
private string _title = "TMIDE";   // genera: public string Title { get; set; }
```

### CommunityToolkit.Mvvm — `[RelayCommand]` (solo en `TmIde`)

Un método privado en `PascalCase` genera una propiedad `IRelayCommand` con el mismo
nombre + sufijo `Command`. Caso real en `MainWindowViewModel`:

```csharp
[RelayCommand]
private void ToggleTheme() { ... }   // genera: public IRelayCommand ToggleThemeCommand { get; }
```

La clase que los use tiene que declararse `partial` (`sealed partial class
MainWindowViewModel : ViewModelBase`).

### Bindings de XAML (WPF)

`{Binding Title}`, `{Binding ToggleThemeCommand}` resuelven por reflexión buscando
el nombre exacto en `PascalCase` de la propiedad generada. Cambiar el estilo de
nomenclatura de las propiedades rompe los bindings de los `.xaml`.

## Pruebas (`Tempo.Language.Tests`)

- Las carpetas reflejan las de la biblioteca (`Syntax/`, `Symbols/`, `Binding/`,
  `Diagnostics/`, `Evaluation/`).
- Nombres de test con el patrón `Metodo_Escenario_ResultadoEsperado`
  (`Tokenize_EmptySource_ReturnsOnlyEndOfFile`). El proyecto de tests silencia
  `CA1707` precisamente para permitir los guiones bajos en esos nombres.
- También silencia `xUnit1012`. Con `Nullable=annotations`, esa regla marcaría
  los `[InlineData(null)]` sobre parámetros `string`, que las pruebas usan para
  cubrir las entradas nulas.
- Framework: xUnit (`[Fact]` / `[Theory]` + `[InlineData]`).

## Estilo de archivos (`.editorconfig`)

| Tipo | `indent_size` | Fin de línea |
|---|---|---|
| `.cs` | 4 | CRLF |
| `.xaml` / `.xml` / `.csproj` / `.slnx` | 2 | CRLF |
| `.md` | — | conserva espacios finales (`trim_trailing_whitespace = false`) |

Todos los archivos: UTF-8, nueva línea final obligatoria.

## Cómo se verifica

```powershell
dotnet build
```

Cualquier violación de las reglas de `.editorconfig` aparece como *error* (por
`TreatWarningsAsErrors`) en la salida de compilación.
