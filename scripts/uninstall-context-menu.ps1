$key = 'Software\Classes\*\shell\UniDocVerter'

try {
    [Microsoft.Win32.Registry]::CurrentUser.DeleteSubKeyTree($key, $false)
    Write-Host "Пункт UniDocVerter удалён."
}
catch {
    Write-Error "Не удалось удалить пункт меню: $($_.Exception.Message)"
    exit 1
}