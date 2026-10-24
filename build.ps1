# Builds the plugin twice, once per Jellyfin server line, and zips each build:
#   .\dist\SeasonalPicks_<base>.12.zip   for Jellyfin 12.x    (net10.0, targetAbi 12.1.0.0)
#   .\dist\SeasonalPicks_<base>.11.zip   for Jellyfin 10.11.x (net9.0,  targetAbi 10.11.0.0)
# Each zip holds the DLL, meta.json, the logo and the font licence, ready for /config/plugins/SeasonalPicks/.
# <base> is the project version without its last part (1.3.1.0 -> 1.3.1). The last part tells the builds apart, and the
# Jellyfin 12 build is always the higher one, so a 12 server picks it even though it could also load the 10.11 build.
$ErrorActionPreference = 'Stop'
$proj = Join-Path $PSScriptRoot 'src\Jellyfin.Plugin.SeasonalPicks\Jellyfin.Plugin.SeasonalPicks.csproj'
$utf8 = New-Object Text.UTF8Encoding $false

# Base version from the project; the newest CHANGELOG.md entry must match it.
$projectVersion = ([xml][IO.File]::ReadAllText($proj)).Project.PropertyGroup.AssemblyVersion | Where-Object { $_ } | Select-Object -First 1
$base = $projectVersion -replace '\.0$', ''
$changelog = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'CHANGELOG.md'), $utf8)
$top = [regex]::Match($changelog, '(?ms)^## (\S+)[^\n]*\n(.*?)(?=^## |\z)')
if (-not $top.Success -or $top.Groups[1].Value -ne $base) {
    throw "CHANGELOG.md's newest entry ($($top.Groups[1].Value)) doesn't match the project version ($base)"
}
$notes = ($top.Groups[2].Value -split "`n" | Where-Object { $_ -match '^- ' } | ForEach-Object { $_.Substring(2).Trim() }) -join ' '

$repo = 'https://github.com/TNT24/jellyfin-seasonal-picks'
$tag = "v$base"
$timestamp = (Get-Date).ToUniversalTime().ToString('o')
$targets = @(
    @{ Server = '12'; Framework = 'net10.0'; Abi = '12.1.0.0'; Revision = 12 },
    @{ Server = '10.11'; Framework = 'net9.0'; Abi = '10.11.0.0'; Revision = 11 }
)

$entries = @()
foreach ($t in $targets) {
    $version = "$base.$($t.Revision)"
    dotnet build $proj -c Release -p:JellyfinTarget=$($t.Server) -p:AssemblyVersion=$version -p:FileVersion=$version
    if ($LASTEXITCODE -ne 0) { throw "Build for Jellyfin $($t.Server) failed" }

    $out = Join-Path $PSScriptRoot "dist\jellyfin-$($t.Server)\SeasonalPicks"
    if (Test-Path $out) { Remove-Item $out -Recurse -Force }
    New-Item -ItemType Directory -Force $out | Out-Null
    Copy-Item (Join-Path $PSScriptRoot "src\Jellyfin.Plugin.SeasonalPicks\bin\Release\$($t.Framework)\Jellyfin.Plugin.SeasonalPicks.dll") $out -Force
    # Licence for the Noto Sans font embedded in the DLL.
    Copy-Item (Join-Path $PSScriptRoot 'src\Jellyfin.Plugin.SeasonalPicks\Fonts\OFL.txt') (Join-Path $out 'NotoSans-OFL.txt') -Force
    # Shown on the plugin's page in the dashboard.
    Copy-Item (Join-Path $PSScriptRoot 'docs\logo.png') $out -Force

    $meta = [ordered]@{
        category    = 'General'
        changelog   = $notes
        description = 'Picks movies from your watch history and the time of year.'
        guid        = '6e3ba9d2-8036-4708-9ce7-35413b443d8f'
        name        = 'Seasonal Picks'
        overview    = 'History- and calendar-aware movie picks as playlists (works on TV), a shared Suggested library, themed seasonal covers and a web Suggested screen.'
        owner       = 'TNT24'
        targetAbi   = $t.Abi
        timestamp   = $timestamp
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
    Write-Host "Jellyfin $($t.Server): $zip"

    $entries += [ordered]@{
        version   = $version
        changelog = $notes
        targetAbi = $t.Abi
        sourceUrl = "$repo/releases/download/$tag/SeasonalPicks_$version.zip"
        checksum  = (Get-FileHash -LiteralPath $zip -Algorithm MD5).Hash.ToLowerInvariant()
        timestamp = $timestamp
    }
}

# manifest.json: the Jellyfin plugin repository. Adds (or replaces) both builds of this version, newest first and the
# Jellyfin 12 build above the 10.11 one, pointing at the zips on the GitHub release tagged v<base>. Commit it after
# uploading both zips to that release.
$manifestPath = Join-Path $PSScriptRoot 'manifest.json'
$old = @()
if (Test-Path $manifestPath) {
    $mine = $entries | ForEach-Object { $_.version }
    $old = @(([IO.File]::ReadAllText($manifestPath, $utf8) | ConvertFrom-Json)[0].versions | Where-Object { $mine -notcontains $_.version })
}
$plugin = [ordered]@{
    guid        = '6e3ba9d2-8036-4708-9ce7-35413b443d8f'
    name        = 'Seasonal Picks'
    description = 'Picks movies from your watch history and the time of year.'
    overview    = 'History- and calendar-aware movie picks as playlists (works on TV), a shared Suggested library, themed seasonal covers and a web Suggested screen.'
    owner       = 'TNT24'
    category    = 'General'
    imageUrl    = 'https://raw.githubusercontent.com/TNT24/jellyfin-seasonal-picks/main/docs/logo.png'
    versions    = @($entries) + $old
}
[IO.File]::WriteAllText($manifestPath, (ConvertTo-Json -InputObject @($plugin) -Depth 5), $utf8)
Write-Host "Manifest: $tag ($(($entries | ForEach-Object { $_.version }) -join ', '))"
