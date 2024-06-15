param (   
    [Parameter()]
    [string]
    $version,

    [Parameter()]
    [ValidateSet('local', 'dev')]
    [string]
    $env
)

#TODO: read from config
$serverPort = 80
if($env -eq 'local') {
    $serverPort = 5001
}

$oldPath = $PWD.Path
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$rootPath = Join-Path $scriptPath "..\"
cd $rootPath
docker build -t "eveniredb:$version" -f "Dockerfile.server" --build-arg SERVER_PORT=$serverPort  .
docker tag "eveniredb:$version" "eveniredb:latest"

docker build -t "eveniredb-adminui:$version" -f "Dockerfile.adminui" --build-arg SERVER_PORT=$serverPort  .
docker tag "eveniredb-adminui:$version" "eveniredb-adminui:latest"
cd $oldPath