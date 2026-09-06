$ErrorActionPreference = 'Stop'
$core = Join-Path $PSScriptRoot '..\Core'
$forbidden = @('Microsoft.Web.WebView2', 'System.Windows', 'Microsoft.Data.Sqlite')
$violations = Get-ChildItem $core -Recurse -Filter *.cs | Select-String -SimpleMatch $forbidden
if ($violations) {
    $violations | ForEach-Object { Write-Error "$($_.Path):$($_.LineNumber): forbidden Core dependency: $($_.Line.Trim())" }
    exit 1
}
Write-Host 'Newton Core architecture boundary check passed.'
