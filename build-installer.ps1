param(
    [string]$Version = "0.1.0"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$project = Join-Path $root "src\Timarker\Timarker.csproj"
$publishDir = Join-Path $root "artifacts\publish"
$installerScript = Join-Path $root "installer\Timarker.iss"
$localDotnet = Join-Path (Split-Path $root -Parent) "DotNetSdk\dotnet.exe"
$dotnet = if (Test-Path $localDotnet) { $localDotnet } else { (Get-Command dotnet -ErrorAction Stop).Source }

$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $root "artifacts"))
$publishDir = [IO.Path]::GetFullPath($publishDir)
if (-not $publishDir.StartsWith($artifactsRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Invalid publish directory: $publishDir"
}
if (Test-Path -LiteralPath $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

& $dotnet publish $project -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false `
    -p:Version=$Version -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "Timarker publish failed." }

$isccCandidates = @(
    [IO.Path]::Combine($env:LOCALAPPDATA, 'Programs\Inno Setup 7\ISCC.exe')
    [IO.Path]::Combine($env:ProgramFiles, 'Inno Setup 7\ISCC.exe')
    [IO.Path]::Combine([Environment]::GetEnvironmentVariable('ProgramFiles(x86)'), 'Inno Setup 6\ISCC.exe')
    [IO.Path]::Combine($env:ProgramFiles, 'Inno Setup 6\ISCC.exe')
)
$iscc = $isccCandidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $iscc) {
    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($command) { $iscc = $command.Source }
}
if (-not $iscc) {
    throw "Inno Setup was not found. Install it, then rerun this script: https://jrsoftware.org/isdl.php"
}

& $iscc "/DAppVersion=$Version" $installerScript
if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed." }

$output = Join-Path $root "artifacts\installer\Timarker-Setup-$Version.exe"
Write-Host "Installer created: $output"
