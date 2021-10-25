$appName = "HealthNotebook"
$scriptFolder = $PSScriptRoot
$homeFolder = Split-Path -Path $scriptFolder -Parent
$dataServiceFolder = $homeFolder + '\' + $appName + '.DataService'
$apiFolder = $homeFolder + '\' + $appName + '.Api'

set-location -Path $dataServiceFolder

dotnet ef database update --startup-project $apiFolder

set-location -Path $scriptFolder