# Tempo

Tempo (**TM**) es un lenguaje de programación imperativo de tipado estático
cuyo vocabulario procede de la música. Este repositorio contiene su
implementación completa: el compilador y el intérprete, una herramienta de
línea de comandos y **TmIde**, un entorno de desarrollo para Windows.

```tm
sample : compose fibonacci(sample n) >
    if [n <= 1] >
        ret n;
    <
    ret fibonacci(n - 1) + fibonacci(n - 2);
<

session >
    output >> "Fib(7) = " + fibonacci(7);
<
```

## Características

**Lenguaje**

- Tipos primitivos `sample` (entero), `wave` (real), `tone` (carácter),
  `phrase` (cadena) y `signal` (booleano), el valor nulo `silence` y las
  colecciones `sequence<T>` y `score<T>`.
- Tipado fuerte sin conversiones implícitas entre tipos numéricos.
- Funciones con recursión, constantes, condicionales, selección múltiple `mix`
  y bucles `loop` y `repeat`.
- Proyectos multiarchivo con un archivo principal `.tmpr` y módulos `.tm`
  enlazados con `import`.
- Catálogo de diagnósticos con código, archivo, línea, descripción y posible
  solución.

**TmIde**

- Editor con coloreado de sintaxis, diagnósticos mientras se escribe,
  autocompletado, búsqueda y reemplazo, plegado de bloques y ayuda contextual.
- Explorador de archivos del proyecto, compilación con diagnósticos navegables y
  ejecución integrada con entrada interactiva.
- Panel de partículas que refleja el estado del proyecto, el ritmo declarado con
  `tempo` y las ráfagas de `pulse`.
- Temas claro y oscuro, proyectos recientes y un piano que suena al escribir.

## Requisitos

| Componente | Requisito |
|---|---|
| SDK | [.NET SDK 10](https://dotnet.microsoft.com/download), versión fijada en `global.json`. |
| Solución completa y TmIde | Windows 10 o posterior. |
| `Tempo.Language` y `Tempo.Cli` | Multiplataforma (Windows, Linux o macOS). |

## Primeros pasos

```powershell
git clone https://github.com/v-arley/tempo-rp.git
cd tempo-rp
dotnet build tempo-rp.slnx
```

### Ejecutar un programa desde la línea de comandos

```powershell
dotnet run --project src/Tempo.Cli -- <directorio | archivo.tmpr | archivo.tm> [--tokens] [--ast]
```

```powershell
dotnet run --project src/Tempo.Cli -- samples/prueba-correcta.tmpr
dotnet run --project src/Tempo.Cli -- samples/importaciones
```

La CLI imprime los diagnósticos de cada archivo y, si el proyecto no tiene
errores, lo ejecuta. Termina con el código `0` si todo fue correcto, `1` ante
errores de compilación y `2` ante un error de ejecución.

### Abrir el entorno de desarrollo

```powershell
dotnet run --project src/TmIde
```

Desde la ventana de inicio se crea un proyecto nuevo, que incluye un
`principal.tmpr` listo para ejecutar, o se abre una carpeta existente.

| Atajo | Acción |
|---|---|
| `Ctrl+S` | Guardar el documento activo |
| `Ctrl+F` / `Ctrl+H` | Buscar / reemplazar |
| `Ctrl+Espacio` | Autocompletar |
| `F1` | Referencia del lenguaje |

Compilar, Ejecutar y Detener están en el menú EJECUCIÓN.

### Ejecutar las pruebas

```powershell
dotnet test tempo-rp.slnx
```

### Generar el ejecutable de TmIde

```powershell
.\publish.ps1                            # Autocontenido: un único TmIde.exe
.\publish.ps1 -Variante Dependiente      # Liviano: requiere .NET Desktop Runtime 10
```

| Variante | Resultado en `publish/` | Requisito en el equipo de destino |
|---|---|---|
| `Autocontenido` | `TmIde.exe` y `appsettings.json` | Ninguno |
| `Dependiente` | Carpeta con el programa y sus bibliotecas | .NET Desktop Runtime 10 |

El parámetro `-Plataforma win-arm64` genera la versión para equipos ARM.

## Estructura del repositorio

```text
tempo-rp/
├── src/
│   ├── Tempo.Language/      Compilador e intérprete de TM
│   ├── Tempo.Cli/           Herramienta de línea de comandos
│   ├── TmIde/               Entorno de desarrollo (WPF)
│   └── Assets/              Fuentes e iconos del IDE
├── tests/
│   ├── Tempo.Language.Tests/
│   └── TmIde.Tests/
├── samples/                 Programas de ejemplo y de diagnóstico
├── docs/                    Documentación técnica
├── publish.ps1              Generación del ejecutable de TmIde
├── tempo-rp.slnx            Solución
├── global.json              Versión del SDK
├── Directory.Build.props    Propiedades comunes de compilación
└── Directory.Packages.props Versiones centralizadas de NuGet
```

## Documentación

| Documento | Contenido |
|---|---|
| [Especificación del lenguaje](docs/ESPECIFICACION-TM.md) | Léxico, gramática, tipos, semántica, proyectos, diagnósticos y ejecución. |
| [Arquitectura](docs/ARQUITECTURA.md) | Diseño del compilador, del intérprete, de la CLI y de TmIde. |
| [Convenciones](docs/CONVENTIONS.md) | Normas de código, diseño, pruebas y control de versiones. |

## Ejemplos

La carpeta [samples/](samples/) contiene programas listos para ejecutar con la
CLI o abrir en TmIde:

| Ejemplo | Muestra |
|---|---|
| `prueba-correcta.tmpr` | Programa válido mínimo. |
| `prueba-comparacion.tmpr`, `prueba-mix-keywords.tmpr` | Comparaciones y selección múltiple. |
| `importaciones/` | Proyecto con un módulo importado. |
| `prueba-error.tmpr`, `prueba-s1.tmpr`, `prueba-s4.tmpr` | Errores de tipo y de sintaxis. |
| `prueba-e011-*.tmpr`, `prueba-e015/` | Errores de estructura del proyecto. |
| `prueba-r001.tmpr`, `prueba-cli.tmpr`, `prueba-r001-importado/` | Errores de ejecución. |
