$ErrorActionPreference = 'Stop'
$core = Join-Path $PSScriptRoot '..\Core'
$forbidden = @('Microsoft.Web.WebView2', 'System.Windows', 'Microsoft.Data.Sqlite')
$violations = Get-ChildItem $core -Recurse -Filter *.cs | Select-String -SimpleMatch $forbidden
if ($violations) {
    $violations | ForEach-Object { Write-Error "$($_.Path):$($_.LineNumber): forbidden Core dependency: $($_.Line.Trim())" }
    exit 1
}
Write-Host 'Newton Core architecture boundary check passed.'

$root = Join-Path $PSScriptRoot '..'
$securityViolations = Get-ChildItem $root -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch '[\\/]obj[\\/]' } | Select-String -Pattern @(
    'IsWebMessageEnabled\s*=\s*true',
    'AreBrowserExtensionsEnabled\s*=\s*true',
    'ServerCertificateErrorAction\.AlwaysAllow'
)
if ($securityViolations) {
    $securityViolations | ForEach-Object { Write-Error "$($_.Path):$($_.LineNumber): prohibited security weakening: $($_.Line.Trim())" }
    exit 1
}
Write-Host 'Newton security-invariant check passed.'
