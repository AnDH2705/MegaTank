param(
    [switch]$NoBrowser,
    [ValidateSet('game', 'animation', 'wolfRig', 'catRig')]
    [string]$View = 'game'
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$gameUrl = 'http://127.0.0.1:5174/'
$viewRoutes = @{ game = ''; animation = 'wolf-frames-preview.html'; wolfRig = 'monster-preview.html'; catRig = 'animation-preview.html' }
$openUrl = $gameUrl + $viewRoutes[$View]
function Get-GameServer {
    try {
        $reply = Invoke-RestMethod -Uri ($gameUrl + '__meow_health') -TimeoutSec 2
        if ($reply.app -eq 'meow-bastion' -and $reply.root -eq $projectRoot) { return $reply }
    } catch {}
    return $null
}
try {
    $running = Get-GameServer
    if (-not $running) {
        $nodeCommand = Get-Command node.exe -ErrorAction Stop
        foreach ($vendor in @('three.module.js', 'three.core.js')) {
            if (-not (Test-Path -LiteralPath (Join-Path $projectRoot "public\vendor\$vendor"))) {
                throw "Missing public/vendor/$vendor. Restore the local Three.js files."
            }
        }
        $serverScript = Join-Path $PSScriptRoot 'server.mjs'
        $serverProcess = Start-Process -FilePath $nodeCommand.Source -ArgumentList @('"' + $serverScript + '"') -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru
        for ($attempt = 0; $attempt -lt 30; $attempt++) {
            Start-Sleep -Milliseconds 200
            $running = Get-GameServer
            if ($running) { break }
            if ($serverProcess.HasExited) { throw 'Cannot start the game. Port 5174 may be used by another application.' }
        }
        if (-not $running) { throw 'The game server did not start at localhost:5174.' }
    }
    if (-not $NoBrowser) { Start-Process -FilePath $openUrl }
    Write-Output "Ready: $openUrl (server PID $($running.pid))"
} catch {
    if (-not $NoBrowser) {
        $popup = New-Object -ComObject WScript.Shell
        $null = $popup.Popup($_.Exception.Message, 0, 'MEOW BASTION - Cannot open game', 16)
    }
    Write-Error $_.Exception.Message
    exit 1
}
