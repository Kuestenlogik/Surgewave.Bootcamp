# Navigate to repo root (one level up from scripts/)
Set-Location (Split-Path $PSScriptRoot -Parent)
$lines = @('<Solution>')

# Units - only include "completed" versions to avoid duplicate project names
$lines += '  <Folder Name="/units/">'
Get-ChildItem units -Recurse -Filter *.csproj |
    Where-Object { $_.FullName -like '*completed*' } |
    ForEach-Object {
        $relativePath = $_.FullName.Substring((Get-Location).Path.Length + 1).Replace('\', '/')
        $lines += "    <Project Path=`"$relativePath`" />"
    }
$lines += '  </Folder>'

# Capstone - only include "completed" version
$lines += '  <Folder Name="/capstone/">'
Get-ChildItem capstone -Recurse -Filter *.csproj |
    Where-Object { $_.FullName -like '*completed*' } |
    ForEach-Object {
        $relativePath = $_.FullName.Substring((Get-Location).Path.Length + 1).Replace('\', '/')
        $lines += "    <Project Path=`"$relativePath`" />"
    }
$lines += '  </Folder>'

$lines += '</Solution>'
$lines | Out-File -FilePath Kuestenlogik.Surgewave.Bootcamp.slnx -Encoding utf8
Write-Host "Generated Kuestenlogik.Surgewave.Bootcamp.slnx with $($lines.Count) lines (completed versions only)"
