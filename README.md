# Tempo Programming Language


### Documentación

- [Especificación formal del lenguaje TM v1.1](docs/ESPECIFICACION-TM.md) —
  gramática léxica y sintáctica (EBNF), sistema de tipos, semántica estática y
  dinámica, formato de errores y suite de pruebas canónicas. Es la referencia
  autoritativa para `Tempo.Language` (Lexer, Parser, análisis semántico e
  intérprete implementados).

- [Arquitectura del proyecto](docs/ARQUITECTURA.md) — estado real del repo: la
  organización por fases de `Tempo.Language` (modelo Roslyn/Minsk), el host
  `Tempo.Cli`, el esqueleto de `TmIde` y la infraestructura de build
  (`global.json`, `Directory.*.props`, `.editorconfig`).

- [Convenciones de código](docs/CONVENTIONS.md) — nomenclatura C#/.NET (estándar
  Microsoft), verificadas en compilación vía `.editorconfig` +
  `EnforceCodeStyleInBuild` + `TreatWarningsAsErrors`.

### Estructura de archivos y directorios

```
tempo-rp/
├── tempo-rp.slnx
├── global.json                          # SDK .NET fijado (10.0.x)
├── Directory.Build.props                # props comunes: Nullable, analizadores, warnings-as-errors
├── Directory.Packages.props             # Central Package Management (versiones NuGet)
├── .editorconfig                        # estilo + nomenclatura C#
├── README.md
├── .gitignore
├── docs/
│   ├── ESPECIFICACION-TM.md
│   ├── ARQUITECTURA.md
│   └── CONVENTIONS.md
├── samples/                             # programas TM de ejemplo (prueba-*.tmpr)
│   └── importaciones/                   # proyecto multiarchivo: principal.tmpr + utilidades.tm
├── src/
│   ├── Assets/                          # fuentes .ttf + iconos .svg compartidos (JetBrains/IBM Plex Mono)
│   ├── Tempo.Language/                  # Biblioteca: TODO el lenguaje, organizada por fase
│   │   ├── Compilation.cs               # fachada: compila un proyecto y lo ejecuta
│   │   ├── Projects/                    # TempoProject, ImportGraph, reglas de proyecto (§6.6)
│   │   ├── Diagnostics/                 # Diagnostic, DiagnosticBag, ProjectDiagnostics, catálogo y códigos (§7)
│   │   ├── Syntax/                      # SyntaxTree, Lexer, Parser, Token/TokenType, AST (33 nodos), *Exception
│   │   ├── Symbols/                     # TmType, TypeKind, TypeNameParser, FunctionSymbol, VariableSymbol
│   │   ├── Binding/                     # ProjectAnalyzer, SemanticAnalyzer, analizadores, SymbolTable/Scope, reglas de tipos
│   │   ├── Evaluation/                  # Interpreter, ejecutores, RuntimeValue/Environment, errores de ejecución
│   │   └── IO/                          # reportes de texto: tokens, AST y diagnósticos
│   ├── Tempo.Cli/                       # Host de consola: pipeline completo + E/S por consola
│   └── TmIde/                           # IDE gráfico (WPF, net10.0-windows, MVVM con
│       ├── Resources/                   #   CommunityToolkit.Mvvm). Esqueleto visual:
│       ├── Services/                    #   chrome propio (WindowChrome), theming en
│       ├── Controls/                    #   caliente, explorador, editor de texto plano,
│       ├── ViewModels/                  #   panel Terminal/Output/Debug, barra de estado.
│       └── Views/
└── tests/
    └── Tempo.Language.Tests/            # xUnit; carpetas espejo de las fases:
        ├── Syntax/                      #   LexerTests, ParserTests
        ├── Symbols/                     #   TmTypeTests, TypeNameParserTests
        ├── Binding/                     #   análisis semántico (+ SemanticTestHarness)
        ├── Diagnostics/                 #   DiagnosticTests
        ├── Evaluation/                  #   intérprete y programas canónicos
        └── Projects/                    #   proyectos multiarchivo (I1–I3, C1) + ProjectWorkspace
```

`Tempo.Language` sigue el modelo de **Roslyn/Minsk** ("el compilador como
API"): un único proyecto con todo el lenguaje, dividido en carpetas y namespaces
según la **fase del pipeline** (texto → `Syntax` → `Binding` → `Evaluation`).
Los namespaces coinciden con la carpeta
(`Tempo.Language.Syntax`, `Tempo.Language.Binding`, …). Los hosts, como
`Tempo.Cli`, consumen la biblioteca a través de la fachada `Compilation`; la
biblioteca no depende de ningún host.

```powershell
dotnet build                                            # compila toda la solución
dotnet test                                             # corre Tempo.Language.Tests
dotnet run --project src/Tempo.Cli -- samples/prueba-correcta.tmpr [--tokens]
dotnet run --project src/Tempo.Cli -- samples/importaciones   # proyecto multiarchivo (directorio)
```

`TmIde` es un proyecto **WPF SDK-style sobre `net10.0-windows`**. Patrón MVVM con
`CommunityToolkit.Mvvm` (`[ObservableProperty]` / `[RelayCommand]`), composición
manual en `App.OnStartup` (sin contenedor de DI), ventana sin chrome nativo con
`System.Windows.Shell.WindowChrome`, y tema claro/oscuro intercambiable en
caliente (15 brushes por rol en `Resources/Theme.*.xaml`, consumidos con
`{DynamicResource}`). El editor es un `TextBox` monoespaciado con canaleta de
números de línea, sin resaltado de sintaxis. El árbol del explorador, las
pestañas del editor y el panel Terminal arrancan vacíos, y los botones de crear
archivo/carpeta y COMPILE / RUN / DEBUG son decorativos (sin acción). `TmIde` no
referencia a `Tempo.Language`.

---
