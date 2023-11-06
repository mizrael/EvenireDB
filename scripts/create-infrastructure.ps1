param (   
    [Parameter()]
    [ValidateSet('none', 'json')]
    [string]
    $verbosity = 'none'
)

$user = az account show --query user.name
if($null -eq $user) {
    az login --output $verbosity
    $user = az account show --query user.name
}

write-host "logged in as $user" -ForegroundColor Green
write-host "deploying resources..." -ForegroundColor Yellow

$location='eastus'
$serviceName='evenire'
$env='dev'

az deployment sub create --location=$location `
    --template-file ./src/infra/main.bicep `
    --parameters service_name=$serviceName location=$location env=$env

# the http20ProxyFlag is not yet supported in the bicep template, so we need to set it manually
$subscriptionId = $(az account show --query id --output tsv)
$resourceGroupName = "rg-$serviceName"
$webAppName = "web-$serviceName-$env"
$uri = "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Web/sites/$webAppName/config/web?api-version=2022-03-01"
az rest `
--method PUT `
--uri $uri `
--body '{\"properties\":{\"http20ProxyFlag\":2}}'    

write-host "deployment complete!" -ForegroundColor Green