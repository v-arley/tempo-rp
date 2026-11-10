# Convenciones

Normas de código, diseño, pruebas y control de versiones del repositorio. Las
reglas de formato y de nombres están declaradas en `.editorconfig` y se
verifican al compilar: una infracción detiene la compilación, porque
`Directory.Build.props` trata las advertencias como errores.

## Contenido

1. [Idioma](#1-idioma)
2. [Formato](#2-formato)
3. [Estilo de C#](#3-estilo-de-c)
4. [Nombres](#4-nombres)
5. [Diseño](#5-diseño)
6. [Interfaz de usuario](#6-interfaz-de-usuario)
7. [Pruebas](#7-pruebas)
8. [Dependencias](#8-dependencias)
9. [Control de versiones](#9-control-de-versiones)

---

## 1. Idioma

| Elemento | Idioma |
|---|---|
| Identificadores de código (tipos, miembros, variables) | Inglés |
| Comentarios y documentación XML | Español |
| Mensajes para el usuario y diagnósticos | Español |
| Documentación del repositorio | Español |
| Mensajes de commit | Español |

Los términos del lenguaje TM (`session`, `compose`, `sample`…) se escriben tal
cual en cualquier contexto.

## 2. Formato

| Regla | Valor |
|---|---|
| Codificación | UTF-8 |
| Fin de línea | CRLF |
| Línea final | Obligatoria |
| Espacios al final de línea | Se eliminan (excepto en Markdown) |
| Sangría | Espacios: 4 en C#; 2 en XAML, XML, `.csproj` y `.slnx` |

## 3. Estilo de C#

- **Espacios de nombres de archivo:** `namespace Tempo.Language.Syntax;`.
- **Llaves en línea propia** (estilo Allman) y **obligatorias** en todo bloque,
  aunque contenga una sola sentencia.
- **Sin `this.`** para acceder a campos, propiedades, métodos o eventos.
- **Modificadores de acceso explícitos** en todos los miembros que no
  pertenecen a una interfaz.
- **`var`** cuando el tipo es evidente o es un tipo integrado.
- **Cuerpos de expresión** para propiedades; los métodos usan cuerpo de bloque.
- **Clases `sealed`** salvo que estén diseñadas para heredarse.
- **Records** para los modelos inmutables (nodos del AST, diagnósticos, tipos,
  preferencias).
- **Nulabilidad.** `Tempo.Language`, `Tempo.Cli` y `Tempo.Language.Tests`
  compilan con `<Nullable>annotations</Nullable>`; `TmIde` y `TmIde.Tests`, con
  `enable`.
- **Registro.** Los mensajes de `ILogger` se declaran con métodos generados por
  `[LoggerMessage]`.
- **Comentarios.** La documentación XML explica el propósito y las decisiones
  que no se deducen del código. Los comentarios en línea son breves y directos.

## 4. Nombres

| Símbolo | Convención | Ejemplo |
|---|---|---|
| Tipos, espacios de nombres, miembros públicos | PascalCase | `ExpressionTypeResolver`, `Resolve` |
| Interfaces | `I` + PascalCase | `IRuntimeOutput` |
| Parámetros de tipo | `T` + PascalCase | `TViewModel` |
| Constantes y campos `static readonly` | PascalCase | `MaxBpm`, `Table` |
| Campos privados (incluidos los estáticos mutables) | `_` + camelCase | `_symbols` |
| Parámetros y variables locales | camelCase | `fileName` |
| Métodos asíncronos | PascalCase + `Async` | `CompileProjectAsync` |

Los nombres son declarativos: describen qué representa el elemento o qué hace,
no cómo está implementado (`ReturnPathAnalyzer.AlwaysReturns`,
`TypeCompatibility.IsAssignable`).

## 5. Diseño

- **Un único compilador.** Toda regla del lenguaje vive en `Tempo.Language`.
  La CLI y el IDE consumen `Compilation` y no reimplementan análisis.
- **Biblioteca independiente del host.** `Tempo.Language` no conoce la consola
  ni la interfaz gráfica: la entrada, la salida y las ráfagas llegan a través de
  `IRuntimeInput`, `IRuntimeOutput` e `IRuntimePulse`.
- **Fuentes únicas.** Cada hecho del lenguaje se define en un solo lugar:
  palabras reservadas en `SyntaxFacts`, títulos de diagnóstico en
  `DiagnosticCatalog`, sugerencias de ejecución en `RuntimeErrorSuggestions`,
  delimitadores de bloque del editor en `TmBlockDelimiters`.
- **Reglas como datos.** Las reglas tabulares se expresan como tablas y no como
  cadenas de condicionales: la tabla de operadores (`OperatorRuleTable`), las
  reglas de proyecto (`ProjectRules.All`) o las formas del panel de partículas
  (`SurfaceShape`). Añadir una regla es añadir una fila.
- **Códigos con nombre.** Los diagnósticos se emiten con las constantes de
  `DiagnosticCodes` y `RuntimeErrorCodes`, nunca con literales.
- **Diagnósticos acumulativos.** El análisis semántico informa todos los
  problemas en una sola pasada y propaga el tipo error (`TmType.Unknown`) en
  silencio para evitar errores en cascada.
- **Patrones con propósito.** Se aplican patrones de diseño cuando simplifican:
  fachada (`Compilation`), estrategia (`IProjectRule`, `ICompletionProvider`),
  compuesto (`CompletionEngine`), adaptador (`TempoCompletionData`).

## 6. Interfaz de usuario

- **MVVM estricto.** Los ViewModels no crean vistas ni dependen de tipos de
  ventana: la navegación pasa por `IFlowService` y los diálogos, por servicios.
- **Comunicación entre regiones** mediante `IMessenger`, sin referencias
  directas entre ViewModels de regiones distintas.
- **Comportamiento de vista** en *Behaviors* de XAML; el *code-behind* se limita
  a lo que es propio de la vista.
- **Temas.** Todo color se define como recurso en `Theme.Dark.xaml` y
  `Theme.Light.xaml`, con las mismas claves en ambos, y se consume con
  `DynamicResource`.
- **Hilos.** El trabajo largo (compilar, ejecutar) se ejecuta fuera del hilo de
  la interfaz; los cambios visibles vuelven a él a través del `Dispatcher`.
- **Mensajes.** Lo que debe ver el usuario va al panel Output (`IOutputLog`); el
  registro técnico va a `ILogger`.

## 7. Pruebas

- **Marco:** xUnit; NSubstitute para dobles de prueba en el IDE.
- **Nombres:** `Metodo_Escenario_Resultado`, por ejemplo
  `Analyze_AssignmentToConstant_ReportsConstantWrite`. Las pruebas asíncronas no
  llevan el sufijo `Async`; las reglas CA1707 y de sufijo asíncrono están
  desactivadas en `tests/`.
- **Estructura:** cada proyecto de pruebas replica las carpetas del proyecto que
  prueba (`Syntax/`, `Binding/`, `Evaluation/`, `ViewModels/`…).
- **Aislamiento:** las pruebas con archivos usan directorios temporales y las
  que dependen del tiempo, un `TimeProvider` fijo.
- **Casos de la especificación:** cada caso de verificación de la sección 10 de
  [ESPECIFICACION-TM.md](ESPECIFICACION-TM.md) tiene su prueba.

```powershell
dotnet test tempo-rp.slnx
```

## 8. Dependencias

- Las versiones de los paquetes NuGet se declaran solo en
  `Directory.Packages.props`; los `.csproj` referencian los paquetes sin
  versión.
- Las dependencias transitivas quedan fijadas por la gestión central
  (`CentralPackageTransitivePinningEnabled`).
- El SDK lo fija `global.json`.

## 9. Control de versiones

### 9.1 Ramas

| Rama | Uso |
|---|---|
| `main` | Versión estable. |
| `develop` | Integración de los cambios terminados. |
| `feature/<tema>` | Nueva funcionalidad. |
| `refactor/<tema>` | Reestructuración sin cambio de comportamiento. |

Los cambios se integran en `develop` mediante *pull request*.

### 9.2 Mensajes de commit

Formato `tipo: descripción`, con la descripción en minúsculas, en infinitivo y
sin punto final:

```text
feat: agregar autocompletado, busqueda y reemplazo al editor
refactor: centralizar las palabras reservadas en SyntaxFacts
```

| Tipo | Uso |
|---|---|
| `feat` | Funcionalidad nueva. |
| `fix` | Corrección de un defecto. |
| `refactor` | Cambio interno sin alterar el comportamiento. |
| `style` | Apariencia de la interfaz o formato del código. |
| `test` | Pruebas. |
| `docs` | Documentación. |
| `build` | Proyectos, paquetes o configuración de compilación. |
