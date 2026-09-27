# Builds the plugin into .\dist\SeasonalPicks\ (DLL, meta.json, font licence) and zips it as .\dist\SeasonalPicks_<version>.zip,
# ready to copy to /config/plugins/SeasonalPicks/ on the server.
$ErrorActionPreference = 'Stop'
$proj = Join-Path $PSScriptRoot 'src\Jellyfin.Plugin.SeasonalPicks\Jellyfin.Plugin.SeasonalPicks.csproj'
$out = Join-Path $PSScriptRoot 'dist\SeasonalPicks'
$utf8 = New-Object Text.UTF8Encoding $false

dotnet build $proj -c Release
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

New-Item -ItemType Directory -Force $out | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'src\Jellyfin.Plugin.SeasonalPicks\bin\Release\net10.0\Jellyfin.Plugin.SeasonalPicks.dll') $out -Force
# Licence for the Noto Sans font embedded in the DLL.
Copy-Item (Join-Path $PSScriptRoot 'src\Jellyfin.Plugin.SeasonalPicks\Fonts\OFL.txt') (Join-Path $out 'NotoSans-OFL.txt') -Force

# meta.json: version from the project, changelog from the newest CHANGELOG.md entry (which must match the version).
$version = ([xml][IO.File]::ReadAllText($proj)).Project.PropertyGroup.AssemblyVersion | Where-Object { $_ } | Select-Object -First 1
$changelog = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'CHANGELOG.md'), $utf8)
$top = [regex]::Match($changelog, '(?ms)^## (\S+)[^\n]*\n(.*?)(?=^## |\z)')
if (-not $top.Success -or "$($top.Groups[1].Value).0" -ne $version) {
    throw "CHANGELOG.md's newest entry ($($top.Groups[1].Value)) doesn't match the project version ($version)"
}
$notes = ($top.Groups[2].Value -split "`n" | Where-Object { $_ -match '^- ' } | ForEach-Object { $_.Substring(2).Trim() }) -join ' '

$meta = [ordered]@{
    category    = 'General'
    changelog   = $notes
    description = 'Picks movies from your watch history and the time of year.'
    guid        = '6e3ba9d2-8036-4708-9ce7-35413b443d8f'
    name        = 'Seasonal Picks'
    overview    = 'History- and calendar-aware movie picks as playlists (works on TV), a shared Suggested library, themed seasonal covers and a web Suggested screen.'
    owner       = 'Mark'
    targetAbi   = '12.1.0.0'
    timestamp   = (Get-Date).ToUniversalTime().ToString('o')
    version     = $version
    status      = 'Active'
    autoUpdate  = $false
    assemblies  = @('Jellyfin.Plugin.SeasonalPicks.dll')
}
[IO.File]::WriteAllText((Join-Path $out 'meta.json'), ($meta | ConvertTo-Json), $utf8)

$zip = Join-Path $PSScriptRoot "dist\SeasonalPicks_$version.zip"
$files = Get-ChildItem -LiteralPath $out -File | ForEach-Object FullName
Compress-Archive -LiteralPath $files -DestinationPath $zip -Force
Write-Host "Plugin $version`: $zip"
