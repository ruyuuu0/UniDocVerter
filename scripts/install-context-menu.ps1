$ErrorActionPreference = "Stop"

$app = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot "..\artifacts\portable\UniDocVerter.exe")
)

if (-not (Test-Path -LiteralPath $app)) {
    throw "No application found: $app"
}

$key = 'Software\Classes\*\shell\UniDocVerter'

$menu = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($key)
try {
    $menu.SetValue('', 'Open with UniDocVerter')
    $menu.SetValue('Icon', "`"$app`",0")
}
finally {
    $menu.Dispose()
}

$command = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey("$key\command")
try {
    $command.SetValue('', "`"$app`" `"%1`"")
}
finally {
    $command.Dispose()
}

Write-Host "Context menu ADD."
Write-Host "Application: $app"