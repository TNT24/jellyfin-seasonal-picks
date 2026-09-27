# Builds the plugin and drops the DLL in .\dist\SeasonalPicks\ ready to copy to /config/plugins/SeasonalPicks/
$ErrorActionPreference = 'Stop'
$proj = Join-Path $PSScriptRoot 'src\Jellyfin.Plugin.SeasonalPicks\Jellyfin.Plugin.SeasonalPicks.csproj'
$out = Join-Path $PSScriptRoot 'dist\SeasonalPicks'

dotnet build $proj -c Release
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

New-Item -ItemType Directory -Force $out | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'src\Jellyfin.Plugin.SeasonalPicks\bin\Release\net10.0\Jellyfin.Plugin.SeasonalPicks.dll') $out -Force
# Licence for the Noto Sans font embedded in the DLL.
Copy-Item (Join-Path $PSScriptRoot 'src\Jellyfin.Plugin.SeasonalPicks\Fonts\OFL.txt') (Join-Path $out 'NotoSans-OFL.txt') -Force
Write-Host "Plugin DLL: $out\Jellyfin.Plugin.SeasonalPicks.dll"
