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
$httpPort = 80
$grpcPort = 5080
if($env -eq 'local') {
    $httpPort = 16281
    $grpcPort = 16282
}

$oldPath = $PWD.Path
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$rootPath = Join-Path $scriptPath "..\"
cd $rootPath
docker build -t "eveniredb:$version" -f Dockerfile --build-arg HTTP_PORT=$httpPort --build-arg GRPC_PORT=$grpcPort .
docker tag "eveniredb:$version" "eveniredb:latest"
cd $oldPath