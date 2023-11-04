param (   
    [Parameter()]
    [ValidateSet('local', 'dev')]
    [string]
    $env = 'dev'
)

$timestamp = $(get-date -UFormat %s)
$version = "$env-$timestamp"

./scripts/dockerize.ps1 -version $version -env $env
./scripts/deploy-azure.ps1 -version $version