$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "VanillaBugFixes.csproj"
$repo = Split-Path $PSScriptRoot -Parent
$mod = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\..\..\VanillaBugFixes"))

foreach ($version in "1.7", "1.6") {
    Write-Host "Building RimWorld $version ..."
    dotnet build $project -c Release "-p:RimWorldVersion=$version" "-p:BaseIntermediateOutputPath=obj/$version/"
    if ($LASTEXITCODE -ne 0) { throw "Build for RimWorld $version failed" }
}

robocopy (Join-Path $repo "Languages") (Join-Path $mod "Languages") /MIR /NJH /NJS /NFL /NDL /NP | Out-Null
if ($LASTEXITCODE -ge 8) { throw "Copying Languages failed" }
robocopy (Join-Path $repo "About") (Join-Path $mod "About") /E /NJH /NJS /NFL /NDL /NP | Out-Null
if ($LASTEXITCODE -ge 8) { throw "Copying About failed" }
Copy-Item (Join-Path $repo "LoadFolders.xml") (Join-Path $mod "LoadFolders.xml") -Force
$global:LASTEXITCODE = 0
Write-Host "Mod files copied to $mod"
