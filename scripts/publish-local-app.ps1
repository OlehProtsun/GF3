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
$backendWwwroot = Join-Path $backendOutput "wwwroot"
$launcherProject = Join-Path $workspaceRoot "GF3.Launcher\GF3.Launcher.csproj"
$backendProject = Join-Path $workspaceRoot "GF3.WebApi\WebApi.csproj"
$frontendRoot = Join-Path $workspaceRoot "FrontEnd"
$frontendOutput = Join-Path $frontendRoot "dist"
$launcherBuildOutput = Join-Path $workspaceRoot "GF3.Launcher\bin\$Configuration\net10.0-windows\$Runtime"
$launcherFallbackOutput = Join-Path $workspaceRoot "GF3.Launcher\bin\$Configuration\net10.0-windows"
$backendBuildOutput = Join-Path $workspaceRoot "GF3.WebApi\bin\$Configuration\net10.0"
$templatesSource = Join-Path $workspaceRoot "GF3.WebApi\Resources\ExcelTemplate"
$templatesTarget = Join-Path $backendOutput "Resources\ExcelTemplate"

if (Test-Path $bundleRoot) {
    Remove-Item $bundleRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $backendOutput -Force | Out-Null

Write-Host "==> Building launcher"
dotnet build $launcherProject `
    -c $Configuration `
    -r $Runtime `
    /p:UseAppHost=true
Assert-LastExitCode "Launcher build"

Write-Host "==> Building backend"
dotnet build $backendProject `
    -c $Configuration `
    /p:UseAppHost=true
if ($LASTEXITCODE -ne 0) {
    $existingBackendExecutable = Join-Path $backendBuildOutput "WebApi.exe"
    if (-not (Test-Path $existingBackendExecutable)) {
        Assert-LastExitCode "Backend build"
    }

    Write-Warning "Backend build failed in the current environment. Reusing the existing backend output from $backendBuildOutput."
}

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

Write-Host "==> Building frontend"
Push-Location $frontendRoot
try {
    npm run build
    if ($LASTEXITCODE -ne 0) {
        if (-not (Test-Path (Join-Path $frontendOutput "index.html"))) {
            Assert-LastExitCode "Frontend build"
        }

        Write-Warning "Frontend build failed in the current environment. Reusing the existing frontend output from $frontendOutput."
    }
}
finally {
    Pop-Location
}

if (-not (Test-Path $launcherBuildOutput)) {
    if (-not (Test-Path $launcherFallbackOutput)) {
        throw "Launcher build output was not found."
    }

    $launcherBuildOutput = $launcherFallbackOutput
}

if (-not (Test-Path $backendBuildOutput)) {
    throw "Backend build output was not found."
}

if (-not (Test-Path $frontendOutput)) {
    throw "Frontend build output was not found."
}

Write-Host "==> Copying launcher bundle"
Copy-DirectoryContents -SourceDirectory $launcherBuildOutput -TargetDirectory $bundleRoot

Write-Host "==> Copying backend bundle"
Copy-DirectoryContents -SourceDirectory $backendBuildOutput -TargetDirectory $backendOutput

Write-Host "==> Copying frontend bundle into backend\\wwwroot"
Copy-DirectoryContents -SourceDirectory $frontendOutput -TargetDirectory $backendWwwroot

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
