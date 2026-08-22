# Tempo Programming Language

Lorem Impsun

### Estructura de archivos y directorios

```
Tempo/
├── Tempo.sln
├── README.md
├── .gitignore
├── docs/
│   ├── especificacion-lenguaje.md
├── src/
│   ├── Tempo.Compiler/
│   │   ├── Tempo.Compiler.csproj
│   │   ├── Lexer/
│   │   │   ├── TokenType.cs
│   │   │   ├── Token.cs
│   │   │   └── TempoLexer.cs
│   │   ├── Parser/
│   │   │   ├── Ast/
│   │   │   │   ├── AstNode.cs
│   │   │   │   ├── StatementNodes.cs
│   │   │   │   └── ExpressionNodes.cs
│   │   │   ├── TempoParser.cs
│   │   │   └── GrammarRules.cs
│   │   ├── Semantic/
│   │   │   ├── SymbolTable.cs
│   │   │   ├── Scope.cs
│   │   │   ├── TypeChecker.cs
│   │   │   └── SemanticAnalyzer.cs
│   │   ├── Diagnostics/
│   │   └── Services/
│   ├── Tempo.Interpreter/
│   ├── Tempo.ProjectSystem/
│   └── Tempo.IDE/
│       ├── Views/
│       ├── ViewModels/
│       ├── Models/
│       ├── Services/
│       ├── Controls/
│       └── Resources/
├── tests/
├── samples/
└── scripts/
```
---
