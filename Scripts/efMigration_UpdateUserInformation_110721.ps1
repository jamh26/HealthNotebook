$appName = "HealthNotebook"
$scriptFolder = $PSScriptRoot
$homeFolder = Split-Path -Path $scriptFolder -Parent
$dataServiceFolder = $homeFolder + '\' + $appName + '.DataService'
$apiFolder = $homeFolder + '\' + $appName + '.Api'

set-location -Path $dataServiceFolder

$commentToAdd = "Update user information"

dotnet ef migrations add $commentToAdd --startup-project $apiFolder
dotnet ef database update --startup-project $apiFolder