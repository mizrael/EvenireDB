$oldPath = $PWD.Path
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$rootPath = Join-Path $scriptPath "..\"
cd $rootPath
docker build -t eveniredb -f Dockerfile .
cd $oldPath