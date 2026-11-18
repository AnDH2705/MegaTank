[CmdletBinding(SupportsShouldProcess=$true)]
param(
    [Parameter(Mandatory=$true)][string]$ProjectPath,
    [string]$KitRoot = ''
)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($KitRoot)) { $KitRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path) }
$project = [IO.Path]::GetFullPath($ProjectPath)
if (-not (Test-Path -LiteralPath (Join-Path $project 'ProjectSettings\ProjectVersion.txt'))) { throw 'ProjectPath is not a Unity project root.' }
$source = Join-Path $KitRoot 'Unity\Assets\CatTanksMockupKit'
$target = Join-Path $project 'Assets\CatTanksMockupKit'
if (Test-Path -LiteralPath $target) { throw 'Assets/CatTanksMockupKit already exists. Choose a clean project or back up that folder before installing a new version.' }
if ($PSCmdlet.ShouldProcess($target, 'Copy standalone mockup asset kit')) {
    Copy-Item -LiteralPath $source -Destination $target -Recurse -ErrorAction Stop
    Write-Output ('Installed: ' + $target)
    Write-Output 'Open Unity and run CatTanks > Mockup Kit > 1 - Import and Slice Assets.'
}
