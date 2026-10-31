$key = 'Software\Classes\*\shell\UniDocVerter'

try {
    [Microsoft.Win32.Registry]::CurrentUser.DeleteSubKeyTree($key, $false)
    Write-Host "The UniDocVerter item has been removed."
}
catch {
    Write-Error "The menu item could not be deleted: $($_.Exception.Message)"
    exit 1
}