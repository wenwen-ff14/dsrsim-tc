#Requires -Version 7.0
param([string]$Changelog = '')

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'AnoMech/AnoMech.csproj'
$projectXml = [xml](Get-Content -Raw -LiteralPath $project)
$version = [string]$projectXml.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+\.\d+$') { throw 'Expected a four-part plugin version.' }

$packageRelativePath = "packages/dsrsim-tc/$version.zip"
$packagePath = Join-Path $repoRoot $packageRelativePath
if (Test-Path -LiteralPath $packagePath) {
    throw "Version $version is already packaged. Increase the project version before preparing an update."
}

# A fresh output directory avoids stale or locked packaging files in OneDrive.
$buildOutput = Join-Path $repoRoot "AnoMech/bin/Repository/$version-$([Guid]::NewGuid().ToString('N'))/"
& dotnet build $project -c Release -p:RestoreLockedMode=true "-p:OutputPath=$buildOutput"
if ($LASTEXITCODE -ne 0) { throw 'Plugin build failed.' }

$manifestPath = Join-Path $buildOutput 'dsrsim-tc/dsrsim-tc.json'
$zipPath = Join-Path $buildOutput 'dsrsim-tc/latest.zip'
$manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json -AsHashtable
$assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $buildOutput 'dsrsim-tc.dll')).Version.ToString()
if ($manifest.InternalName -ne 'dsrsim-tc' -or $manifest.DalamudApiLevel -ne 13 -or
    $manifest.AssemblyVersion -ne $version -or $assemblyVersion -ne $version) {
    throw 'Manifest, assembly version or Dalamud API level mismatch.'
}

$zip = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    foreach ($required in @('dsrsim-tc.dll', 'dsrsim-tc.json', 'dsrsim-tc.deps.json')) {
        if ($null -eq $zip.GetEntry($required)) { throw "Missing package entry: $required" }
    }
    $reader = [IO.StreamReader]::new($zip.GetEntry('dsrsim-tc.json').Open())
    try { $packedManifest = $reader.ReadToEnd() | ConvertFrom-Json }
    finally { $reader.Dispose() }
    if ($packedManifest.AssemblyVersion -ne $version -or $packedManifest.DalamudApiLevel -ne 13) {
        throw 'Packaged manifest does not match the build.'
    }
} finally { $zip.Dispose() }

$downloadUrl = "https://raw.githubusercontent.com/wenwen-ff14/test/main/$packageRelativePath"
$manifest.DownloadLinkInstall = $downloadUrl
$manifest.DownloadLinkUpdate = $downloadUrl
$manifest.DownloadLinkTesting = $downloadUrl
$manifest.IsHide = $false
$manifest.IsTestingExclusive = $false
$manifest.LastUpdate = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
if ($Changelog) { $manifest.Changelog = $Changelog }

New-Item -ItemType Directory -Path (Split-Path -Parent $packagePath) -Force | Out-Null
Copy-Item -LiteralPath $zipPath -Destination $packagePath
$repositoryJson = ConvertTo-Json -InputObject @($manifest) -Depth 10
[IO.File]::WriteAllText((Join-Path $repoRoot 'pluginmaster.json'), $repositoryJson + "`n", [Text.UTF8Encoding]::new($false))
Write-Host "Prepared $packageRelativePath and pluginmaster.json. Commit and push both to publish."
