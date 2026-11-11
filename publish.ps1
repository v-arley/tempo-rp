<#
.SYNOPSIS
    Genera el ejecutable distribuible de TmIde (TempoIDE.exe).

.DESCRIPTION
    Publica TmIde en modo Release para Windows en la carpeta publish/<variante>.

    Autocontenido: un único TempoIDE.exe que incluye el runtime de .NET; no requiere
    nada instalado en el equipo de destino. Se distribuye junto a appsettings.json.

    Dependiente: una carpeta liviana que requiere el .NET Desktop Runtime 10
    instalado en el equipo de destino. Se distribuye la carpeta completa.

.PARAMETER Variante
    Autocontenido (por defecto) o Dependiente.

.PARAMETER Plataforma
    Identificador de plataforma de .NET: win-x64 (por defecto) o win-arm64.

.EXAMPLE
    .\publish.ps1

.EXAMPLE
    .\publish.ps1 -Variante Dependiente -Plataforma win-arm64
#>
[CmdletBinding()]
param(
    [ValidateSet('Autocontenido', 'Dependiente')]
    [string] $Variante = 'Autocontenido',

    [ValidateSet('win-x64', 'win-arm64')]
    [string] $Plataforma = 'win-x64'
)

$ErrorActionPreference = 'Stop'

$raiz = $PSScriptRoot
$proyecto = Join-Path $raiz 'src\TmIde\TmIde.csproj'
$salida = Join-Path $raiz "publish\$($Variante.ToLowerInvariant())-$Plataforma"

# Un TmIde abierto desde el repositorio bloquea los archivos de la compilación.
if (Get-Process -Name 'TempoIDE' -ErrorAction SilentlyContinue) {
    throw 'TempoIDE está en ejecución. Ciérrelo antes de publicar.'
}

$argumentos = @(
    'publish', $proyecto,
    '-c', 'Release',
    '-r', $Plataforma,
    '-o', $salida,
    '-p:DebugType=None',
    '-clp:ErrorsOnly'
)

if ($Variante -eq 'Autocontenido') {
    $argumentos += @(
        '--self-contained', 'true',
        '-p:PublishSingleFile=true',
        '-p:IncludeNativeLibrariesForSelfExtract=true'
    )
}
else {
    $argumentos += @('--self-contained', 'false')
}

# La salida anterior se reemplaza para no mezclar archivos de otra publicación.
if (Test-Path $salida) {
    Remove-Item -Path $salida -Recurse -Force
}

Write-Host "Publicando TmIde ($Variante, $Plataforma)..." -ForegroundColor Cyan
& dotnet @argumentos

if ($LASTEXITCODE -ne 0) {
    throw "La publicación falló (código $LASTEXITCODE)."
}

$archivos = Get-ChildItem -Path $salida -Recurse -File
$tamano = ($archivos | Measure-Object -Property Length -Sum).Sum / 1MB

Write-Host ''
Write-Host 'Publicación completada.' -ForegroundColor Green
Write-Host ("  Carpeta:  {0}" -f $salida)
Write-Host ("  Archivos: {0}, {1:N1} MB" -f $archivos.Count, $tamano)

if ($Variante -eq 'Autocontenido') {
    Write-Host '  Distribuya TempoIDE.exe junto con appsettings.json.'
}
else {
    Write-Host '  Distribuya la carpeta completa. Requiere el .NET Desktop Runtime 10.'
}
