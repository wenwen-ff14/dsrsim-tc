param(
    [Parameter(Mandatory = $true)]
    [string]$GameExe
)

$ErrorActionPreference = 'Stop'
$bytes = [IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $GameExe))
$peOffset = [BitConverter]::ToInt32($bytes, 0x3C)
$sectionCount = [BitConverter]::ToUInt16($bytes, $peOffset + 6)
$optionalSize = [BitConverter]::ToUInt16($bytes, $peOffset + 20)
$sectionTable = $peOffset + 24 + $optionalSize
$textBytes = $null
for ($i = 0; $i -lt $sectionCount; $i++) {
    $section = $sectionTable + 40 * $i
    $name = [Text.Encoding]::ASCII.GetString($bytes, $section, 8).Trim([char]0)
    if ($name -eq '.text') {
        $size = [BitConverter]::ToInt32($bytes, $section + 16)
        $offset = [BitConverter]::ToInt32($bytes, $section + 20)
        $textBytes = [Text.Encoding]::Latin1.GetString($bytes, $offset, $size)
        break
    }
}
if ($null -eq $textBytes) { throw 'The executable has no .text section.' }

$source = Join-Path $PSScriptRoot '../AnoMech'
$pattern = '"((?:[0-9A-F]{2}|\?\?)(?: (?:[0-9A-F]{2}|\?\?)){3,})"'
$failed = 0
Get-ChildItem -LiteralPath $source -Filter '*.cs' -Recurse | ForEach-Object {
    $file = $_
    $code = [IO.File]::ReadAllText($file.FullName)
    foreach ($match in [regex]::Matches($code, $pattern)) {
        $signature = $match.Groups[1].Value
        $regex = ($signature.Split(' ') | ForEach-Object {
            if ($_ -eq '??') { '.' } else { '\x' + $_ }
        }) -join ''
        $hits = [regex]::Matches($textBytes, $regex, [Text.RegularExpressions.RegexOptions]::Singleline).Count
        if ($hits -ne 1) { $failed++ }
        [pscustomobject]@{ File = $file.Name; Matches = $hits; Signature = $signature }
    }
}
if ($failed -gt 0) { throw "$failed signatures do not have exactly one match. This checks bytes only, not native calling conventions or layouts." }
