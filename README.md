# Tempo Programming Language


### Documentación

- [Especificación formal del lenguaje TM v1.1](docs/ESPECIFICACION-TM.md) —
  gramática léxica y sintáctica (EBNF), sistema de tipos, semántica estática y
  dinámica, formato de errores y suite de pruebas canónicas. Es la referencia
  autoritativa para `TmCompiler` (Lexer, y próximamente Parser/análisis
  semántico).

- [Arquitectura del proyecto](docs/ARQUITECTURA.md) — estado real del repo: las
  capas de `TmCompiler`, el diseño del `Lexer`, el esqueleto de `TmIde` y la
  infraestructura de build (`global.json`, `Directory.*.props`, `.editorconfig`).

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
├── src/
│   ├── Assets/                          # fuentes .ttf + iconos .svg compartidos (JetBrains/IBM Plex Mono)
│   ├── TmCompiler/
│   │   ├── TmCompiler.Domain/            # Modelos puros, sin dependencias externas
│   │   │   ├── Lexical/                  # IToken, Token, TokenType, LexerException
│   │   │   ├── Syntactic/                # AST (pendiente: Parser)
│   │   │   └── Semantic/                 # SymbolTable, etc. (pendiente: análisis semántico)
│   │   ├── TmCompiler.Application/       # Casos de uso — depende solo de Domain
│   │   │   ├── Lexing/                   # ILexer, Lexer
│   │   │   ├── Parsing/                  # (pendiente)
│   │   │   └── Analysis/                 # (pendiente)
│   │   ├── TmCompiler.Infrastructure/    # Servicios externos — depende de Domain
│   │   │   └── LexerDiagnosticReporter.cs
│   │   └── TmCompiler.Cli/               # Arnés de consola para probar el pipeline
│   ├── TmIde/                            # IDE gráfico (WPF, net10.0-windows, MVVM con
│   │   ├── Resources/                    #   CommunityToolkit.Mvvm). Esqueleto visual:
│   │   ├── Services/                     #   chrome propio (WindowChrome), theming en
│   │   ├── Controls/                     #   caliente, explorador, editor de texto plano,
│   │   ├── ViewModels/                   #   panel Terminal/Output/Debug, barra de estado.
│   │   └── Views/
│   ├── TmInterpreter/                    # Placeholder — sin implementar aún
│   └── TmSystemProject/                  # Placeholder — sin implementar aún
└── tests/
    └── TmCompiler.Tests/
        └── Lexing/
            └── LexerTests.cs
```

La arquitectura de `TmCompiler` sigue Clean Architecture por capas: `Domain` no
depende de nada, `Application` depende solo de `Domain`, e `Infrastructure`
depende de `Domain` (y de `Application` cuando haga falta). Esa regla de
dependencias es verificable por el compilador — cada capa es su propio
proyecto, así que una capa interna físicamente no puede referenciar una capa
externa.

`TmIde` es un proyecto **WPF SDK-style sobre `net10.0-windows`**. Patrón MVVM con
`CommunityToolkit.Mvvm` (`[ObservableProperty]` / `[RelayCommand]`), composición
manual en `App.OnStartup` (sin contenedor de DI), ventana sin chrome nativo con
`System.Windows.Shell.WindowChrome`, y tema claro/oscuro intercambiable en
caliente (15 brushes por rol en `Resources/Theme.*.xaml`, consumidos con
`{DynamicResource}`). El editor es un `TextBox` monoespaciado con canaleta de
números de línea; no aplica resaltado de sintaxis todavía. El árbol del
explorador, las pestañas del editor y el panel Terminal arrancan vacíos, y los
botones de crear archivo/carpeta y COMPILE / RUN / DEBUG son hoy solo
decorativos (sin acción). Nada de `TmIde` referencia aún a `TmCompiler.*`.

`TmInterpreter` y `TmSystemProject` no tienen código todavía: se dividirán en
las mismas capas (Domain/Application/Infrastructure) cuando arranque su
implementación, siguiendo el mismo patrón que `TmCompiler`.

---
