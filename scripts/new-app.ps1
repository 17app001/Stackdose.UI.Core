param(
    [Parameter(Mandatory = $true, HelpMessage = "App 名稱，例如 Stackdose.App.ModelF")]
    [string]$AppName,
    [ValidateSet("SinglePage", "Standard", "Dashboard")]
    [string]$Mode = "SinglePage",
    [string]$DestinationRoot = (Join-Path $PSScriptRoot "..\..")
)

& "$PSScriptRoot\init-shell-app.ps1" `
    -AppName            $AppName `
    -DestinationRoot    $DestinationRoot `
    -JsonDrivenApp `
    -JsonDrivenShellMode $Mode
