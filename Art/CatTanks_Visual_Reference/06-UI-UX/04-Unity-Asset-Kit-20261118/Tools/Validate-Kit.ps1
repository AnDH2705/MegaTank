param([string]$KitRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
if (-not ('KitPngInspector' -as [type])) { Add-Type -Path (Join-Path $PSScriptRoot 'InspectPng.cs') -ReferencedAssemblies System.Drawing }
$assetRoot = Join-Path $KitRoot 'Unity\Assets\CatTanksMockupKit'
$manifest = Get-Content -LiteralPath (Join-Path $assetRoot 'Data\asset-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$layouts = Get-Content -LiteralPath (Join-Path $assetRoot 'Data\screen-layouts.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$known = @{}
$reports = @()
$issues = @()
foreach ($sheet in $manifest.sheets) {
    $path = Join-Path $assetRoot $sheet.file
    $report = [KitPngInspector]::Inspect($path, $sheet.columns, $sheet.rows)
    $reports += [PSCustomObject]@{id=$sheet.id; width=$report.width; height=$report.height; cells=$report.cells}
    if ($sheet.keys.Count -ne $sheet.columns * $sheet.rows) { $issues += ('Invalid grid count: ' + $sheet.id) }
    foreach ($cell in $report.cells) {
        $key = $sheet.keys[$cell.index]
        if ($known.ContainsKey($key)) { $issues += ('Duplicate key: ' + $key) }
        $known[$key] = $true
        if ($cell.width -eq 0 -or $cell.height -eq 0) { $issues += ('Blank sprite: ' + $key) }
        if ($sheet.alpha -and $cell.transparentFraction -lt 0.02) { $issues += ('No transparent gutters: ' + $key) }
        if ($sheet.alpha -and $cell.touchesCellBoundary) { $issues += ('Sprite touches grid edge: ' + $key) }
    }
}
foreach ($original in $manifest.originals) {
    if (-not (Test-Path -LiteralPath (Join-Path $assetRoot $original.file) -PathType Leaf)) { $issues += ('Missing original: ' + $original.key) }
    if ($known.ContainsKey($original.key)) { $issues += ('Duplicate original: ' + $original.key) }
    $known[$original.key] = $true
}
$screenKeys = @{}
foreach ($screen in $layouts.screens) { $screenKeys[$screen.key] = $true }
foreach ($screen in $layouts.screens) {
    $parents = @{}
    foreach ($node in $screen.nodes) {
        if ($parents.ContainsKey($node.id)) { $issues += ('Duplicate layout node: ' + $screen.key + '/' + $node.id) }
        if ($node.parent -and -not $parents.ContainsKey($node.parent)) { $issues += ('Missing or late parent: ' + $screen.key + '/' + $node.id) }
        $parents[$node.id] = $node
        if ($node.sprite -and -not $known.ContainsKey($node.sprite)) { $issues += ('Missing sprite: ' + $screen.key + '/' + $node.sprite) }
        if ($node.kind -eq 'bar' -and -not $known.ContainsKey($node.text)) { $issues += ('Missing fill: ' + $node.text) }
        if ($node.w -le 0 -or $node.h -le 0) { $issues += ('Invalid size: ' + $screen.key + '/' + $node.id) }
        if (-not $node.parent -and ($node.x -lt 0 -or $node.y -lt 0 -or ($node.x + $node.w) -gt 1600 -or ($node.y + $node.h) -gt 900)) { $issues += ('Outside 1600x900: ' + $screen.key + '/' + $node.id) }
        if ($node.action -like 'screen:*' -and -not $screenKeys.ContainsKey($node.action.Substring(7))) { $issues += ('Broken navigation: ' + $node.action) }
    }
    if ($screen.popup -and -not $screenKeys.ContainsKey($screen.parentKey)) { $issues += ('Missing popup background: ' + $screen.key) }
}
$result = [PSCustomObject]@{checkedAt=[DateTime]::UtcNow.ToString('o');generatedTextures=$manifest.sheets.Count;spriteCount=$known.Count;screens=$layouts.screens.Count;issues=$issues;images=$reports;unityEditorExecuted=$false}
$result | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $KitRoot 'asset-validation.json') -Encoding UTF8
$result | Select-Object generatedTextures,spriteCount,screens,@{Name='IssueCount';Expression={$_.issues.Count}}
if ($issues.Count -gt 0) { $issues | ForEach-Object { Write-Output $_ }; exit 1 }
