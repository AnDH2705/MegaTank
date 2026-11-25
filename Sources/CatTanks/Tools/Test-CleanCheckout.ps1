param([string]$UnityPath)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$repoRoot = (& git -C $projectRoot rev-parse --show-toplevel).Trim()
$prefix = (& git -C $projectRoot rev-parse --show-prefix).Trim().TrimEnd('/')
$revision = (& git -C $projectRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot read the committed project.' }
$versionLine = Get-Content -LiteralPath (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt') | Where-Object { $_ -match '^m_EditorVersion: ' }
$version = ($versionLine -replace '^m_EditorVersion: ', '').Trim()
if (-not $UnityPath) { $UnityPath = Join-Path $env:ProgramFiles "Unity/Hub/Editor/$version/Editor/Unity.exe" }
if (-not (Test-Path -LiteralPath $UnityPath)) { throw "Unity $version is required. Supply -UnityPath if installed elsewhere." }

# Export HEAD, not the working tree: stashed/uncommitted fixes and Library caches
# must never make this check pass on only one developer's machine.
$runRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot ('Temp/PostPull/' + [Guid]::NewGuid().ToString('N'))))
if (-not $runRoot.StartsWith($projectRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid temporary checkout path.' }
New-Item -ItemType Directory -Path $runRoot -Force | Out-Null
$archive = Join-Path $runRoot 'tracked.zip'
$checkoutRoot = Join-Path $runRoot 'checkout'
Write-Host "Exporting committed project $revision"
& git -C $repoRoot archive --format=zip "--output=$archive" HEAD -- $prefix
if ($LASTEXITCODE -ne 0) { throw 'git archive failed.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory($archive, $checkoutRoot)
$cleanProject = [IO.Path]::GetFullPath((Join-Path $checkoutRoot $prefix))
$hadLibrary = Test-Path -LiteralPath (Join-Path $cleanProject 'Library')
if ($hadLibrary) { throw 'A clean checkout must not contain Library.' }
$logs = Join-Path $cleanProject 'Logs'
New-Item -ItemType Directory -Path $logs -Force | Out-Null
$log = Join-Path $logs 'readiness-unity.log'
$testProfile = Join-Path $logs 'readiness-profile.json'
$unityArgs = @('-batchmode', '-projectPath', "`"$cleanProject`"", '-executeMethod', 'CatTanks.Editor.PlayModeReadiness.Run', '-catTanksTestProfile', "`"$testProfile`"", '-screen-width', '1600', '-screen-height', '900', '-logFile', "`"$log`"")
Write-Host "Starting Unity $version without an existing Library: $cleanProject"
$process = Start-Process -FilePath $UnityPath -WorkingDirectory $cleanProject -ArgumentList $unityArgs -WindowStyle Hidden -PassThru
$elapsed = [Diagnostics.Stopwatch]::StartNew()
while (-not $process.WaitForExit(1000)) {
    if ([int]$elapsed.Elapsed.TotalSeconds % 30 -eq 0) { Write-Host "Importing/testing fresh checkout: $([int]$elapsed.Elapsed.TotalSeconds)s; log: $log" }
}
$process.Refresh()
$reportPath = Join-Path $logs 'playmode-readiness.json'
if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $reportPath)) {
    if (Test-Path -LiteralPath $log) { Get-Content -LiteralPath $log | Select-String 'error CS|Exception:|PLAYMODE_READINESS_' | Select-Object -Last 12 | ForEach-Object { Write-Host $_.Line } }
    throw "Clean checkout failed (exit $($process.ExitCode)); inspect $log"
}
$result = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
if ($result.verdict -ne 'PASS' -or -not $result.profileUnchanged) { throw "Readiness failed: $reportPath" }
$result | Add-Member -NotePropertyName revision -NotePropertyValue $revision
$result | Add-Member -NotePropertyName freshCheckout -NotePropertyValue $true
$result | Add-Member -NotePropertyName hadLibraryBeforeImport -NotePropertyValue $hadLibrary
$result | Add-Member -NotePropertyName checkoutPath -NotePropertyValue $cleanProject
New-Item -ItemType Directory -Path (Join-Path $projectRoot 'Logs') -Force | Out-Null
$summary = Join-Path $projectRoot 'Logs/post-pull-clean-readiness.json'
$result | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $summary -Encoding UTF8
Write-Host "PASS: $($result.passed) checks; profile unchanged; report: $summary"
