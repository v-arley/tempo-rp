# Tempo Programming Language


### Documentación

- [Especificación formal del lenguaje TM v1.1](docs/ESPECIFICACION-TM.md) —
  gramática léxica y sintáctica (EBNF), sistema de tipos, semántica estática y
  dinámica, formato de errores y suite de pruebas canónicas. Es la referencia
  autoritativa para `TmCompiler` (Lexer, y próximamente Parser/análisis
  semántico).

### Estructura de archivos y directorios

```
tempo-rp/
├── tempo-rp.slnx
├── README.md
├── .gitignore
├── docs/
│   └── ESPECIFICACION-TM.md
├── src/
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
│   ├── TmIde/                            # Shell WPF/consola (futuro IDE gráfico;
│   │                                        hoy corre en modo consola mientras se
│   │                                        construye el compilador)
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

`TmInterpreter` y `TmSystemProject` no tienen código todavía: se dividirán en
las mismas capas (Domain/Application/Infrastructure) cuando arranque su
implementación, siguiendo el mismo patrón que `TmCompiler`.

---
