$ErrorActionPreference = "Stop"

$app = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot "..\artifacts\portable-test\UniDocVerter.exe")
)

if (-not (Test-Path -LiteralPath $app)) {
    throw "Не найдено приложение: $app"
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

Write-Host "Контекстное меню добавлено."
Write-Host "Приложение: $app"