param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-LastExitCode {
    param(
        [Parameter(Mandatory = $true)]
        [string]$StepName
    )

    if ($LASTEXITCODE -ne 0) {
        throw "$StepName failed with exit code $LASTEXITCODE."
    }
}

function Copy-DirectoryContents {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceDirectory,

        [Parameter(Mandatory = $true)]
        [string]$TargetDirectory
    )

    if (-not (Test-Path $SourceDirectory)) {
        throw "Source directory not found: $SourceDirectory"
    }

    New-Item -ItemType Directory -Path $TargetDirectory -Force | Out-Null
    Copy-Item (Join-Path $SourceDirectory "*") $TargetDirectory -Recurse -Force
}

$workspaceRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $workspaceRoot "artifacts\local-app"
}

$bundleRoot = Join-Path $OutputRoot "app"
$backendOutput = Join-Path $bundleRoot "backend"
$launcherProject = Join-Path $workspaceRoot "GF3.Launcher\GF3.Launcher.csproj"
$backendProject = Join-Path $workspaceRoot "GF3.WebApi\WebApi.csproj"
$frontendRoot = Join-Path $workspaceRoot "FrontEnd"
$templatesSource = Join-Path $workspaceRoot "GF3.WebApi\Resources\ExcelTemplate"
$templatesTarget = Join-Path $backendOutput "Resources\ExcelTemplate"
$dotnetCliHome = Join-Path $workspaceRoot ".dotnet-cli"

$env:DOTNET_CLI_HOME = $dotnetCliHome
New-Item -ItemType Directory -Path $dotnetCliHome -Force | Out-Null

if (Test-Path $bundleRoot) {
    Remove-Item $bundleRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $backendOutput -Force | Out-Null

if (-not (Test-Path (Join-Path $frontendRoot "node_modules"))) {
    Write-Host "==> Installing frontend dependencies"
    Push-Location $frontendRoot
    try {
        npm install
        Assert-LastExitCode "Frontend dependency install"
    }
    finally {
        Pop-Location
    }
}

Write-Host "==> Publishing launcher (self-contained)"
dotnet publish $launcherProject `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    /p:UseAppHost=true `
    /p:PublishSingleFile=false `
    -o $bundleRoot
Assert-LastExitCode "Launcher publish"

Write-Host "==> Publishing backend (self-contained)"
dotnet publish $backendProject `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    /p:UseAppHost=true `
    /p:PublishSingleFile=false `
    -o $backendOutput
Assert-LastExitCode "Backend publish"

if (Test-Path $templatesSource) {
    Write-Host "==> Copying Excel templates to $templatesTarget"
    Copy-DirectoryContents -SourceDirectory $templatesSource -TargetDirectory $templatesTarget
}

Write-Host ""
Write-Host "GF3 local app bundle is ready:"
Write-Host "  $bundleRoot"
Write-Host ""
Write-Host "Start with:"
Write-Host "  $bundleRoot\\GF3.Launcher.exe"
