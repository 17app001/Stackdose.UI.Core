param(
    [Parameter(Mandatory = $true, HelpMessage = "App 名稱，例如 Stackdose.App.ModelF")]
    [string]$AppName,

    # 預設 SinglePage；Standard = 多頁 + LeftNav
    [ValidateSet("SinglePage", "Standard")]
    [string]$Mode = "SinglePage",

    # 預設放在 UI.Core 的上一層（與 Stackdose.App.ModelE 同層）
    [string]$DestinationRoot = (Join-Path $PSScriptRoot "..\..")
)

& "$PSScriptRoot\init-shell-app.ps1" `
    -AppName            $AppName `
    -DestinationRoot    $DestinationRoot `
    -JsonDrivenApp `
    -JsonDrivenShellMode $Mode
