# Builds the plugin into .\dist\SeasonalPicks\ (DLL, meta.json, logo, font licence) and zips it as .\dist\SeasonalPicks_<version>.zip,
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
# Shown on the plugin's page in the dashboard.
Copy-Item (Join-Path $PSScriptRoot 'docs\logo.png') $out -Force

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
    # Lets Jellyfin's daily Update Plugins task install new releases from the repository by itself.
    autoUpdate  = $true
    assemblies  = @('Jellyfin.Plugin.SeasonalPicks.dll')
    imagePath   = 'logo.png'
}
[IO.File]::WriteAllText((Join-Path $out 'meta.json'), ($meta | ConvertTo-Json), $utf8)

$zip = Join-Path $PSScriptRoot "dist\SeasonalPicks_$version.zip"
$files = Get-ChildItem -LiteralPath $out -File | ForEach-Object FullName
Compress-Archive -LiteralPath $files -DestinationPath $zip -Force
Write-Host "Plugin $version`: $zip"

# manifest.json: the Jellyfin plugin repository. Adds (or replaces) this version, pointing at the zip on the GitHub release
# tagged v<major.minor.patch>. Commit it after uploading the zip to that release.
$repo = 'https://github.com/TNT24/jellyfin-seasonal-picks'
$tag = 'v' + ($version -replace '\.0$', '')
$manifestPath = Join-Path $PSScriptRoot 'manifest.json'
$entry = [ordered]@{
    version   = $version
    changelog = $notes
    targetAbi = $meta.targetAbi
    sourceUrl = "$repo/releases/download/$tag/SeasonalPicks_$version.zip"
    checksum  = (Get-FileHash -LiteralPath $zip -Algorithm MD5).Hash.ToLowerInvariant()
    timestamp = $meta.timestamp
}
$old = @()
if (Test-Path $manifestPath) {
    $old = @(([IO.File]::ReadAllText($manifestPath, $utf8) | ConvertFrom-Json)[0].versions | Where-Object { $_.version -ne $version })
}
$plugin = [ordered]@{
    guid        = $meta.guid
    name        = $meta.name
    description = $meta.description
    overview    = $meta.overview
    owner       = $meta.owner
    category    = $meta.category
    imageUrl    = 'https://raw.githubusercontent.com/TNT24/jellyfin-seasonal-picks/main/docs/logo.png'
    versions    = @($entry) + $old
}
[IO.File]::WriteAllText($manifestPath, (ConvertTo-Json -InputObject @($plugin) -Depth 5), $utf8)
Write-Host "Manifest: $tag ($($entry.checksum))"
