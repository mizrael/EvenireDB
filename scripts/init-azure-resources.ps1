param (
    [Parameter()]
    [string]
    $resourceGroupName = 'rg-evenire',

    [Parameter()]
    [string]
    $registryName = 'crevenire',
    
    [Parameter()]
    [string]
    $location = 'eastus',

    [Parameter()]
    [string]
    $planName = 'plan-evenire',

    [Parameter()]
    [string]
    $webAppName = 'web-evenire',
    
    [Parameter()]
    [ValidateSet('none', 'json')]
    [string]
    $verbosity = 'none'
)

$user = az account show --query user.name
if($user -eq $null) {
    az login --output $verbosity
    $user = az account show --query user.name
}

write-host "logged in as $user" -ForegroundColor Green

write-host "ensuring resource group '$resourceGroupName' ..." -ForegroundColor Yellow
az group create --name $resourceGroupName --location $location --output $verbosity

write-host "ensuring container registry '$registryName' in resource group '$resourceGroupName' ..." -ForegroundColor Yellow
az acr create --name $registryName --resource-group $resourceGroupName --sku standard --admin-enabled true --output $verbosity
# TODO: grant permissions (acrPush acrDelete)

write-host "ensuring service plan '$planName' in resource group '$resourceGroupName' ..." -ForegroundColor Yellow
az appservice plan create --name $planName --resource-group $resourceGroupName --is-linux --sku F1

write-host "ensuring webapp '$webAppName' in resource group '$resourceGroupName' ..." -ForegroundColor Yellow
az webapp create --resource-group $resourceGroupName --plan $planName --name $webAppName --deployment-container-image-name "$registryName.azurecr.io/eveniredb:latest"

# TODO: appinsights