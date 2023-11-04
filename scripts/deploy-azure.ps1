param (
    [Parameter()]
    [string]
    $version,

    [Parameter()]
    [string]
    $registryName = 'crevenire',
    
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

write-host "logging on the container registry ..." -ForegroundColor Yellow
$token = $(az acr login --name $registryName --expose-token --only-show-errors --output tsv --query accessToken)
docker login "$registryName.azurecr.io" --username 00000000-0000-0000-0000-000000000000 --password $token

write-host "deploying EvenireDB docker image ..." -ForegroundColor Yellow

docker tag "eveniredb:$version" "$registryName.azurecr.io/eveniredb:$version"
docker tag "eveniredb:latest" "$registryName.azurecr.io/eveniredb:latest"

docker push "$registryName.azurecr.io/eveniredb:$version"
docker push "$registryName.azurecr.io/eveniredb:latest"

write-host "deployment complete!" -ForegroundColor Green