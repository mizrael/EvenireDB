$oldPath = $PWD.Path
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$rootPath = Join-Path $scriptPath "..\"

cd $rootPath
if(Test-Path -Path ./tests/TestResults){
    rm ./tests/TestResults -Force -Confirm:$false -Recurse
} 

cd src

dotnet test --no-build -m:1 -s ../tests/tests.runsettings `
    /p:CollectCoverage=true `
    /p:CoverletOutput=../TestResults/ `
    /p:MergeWith="../TestResults/coverage.json" `
    --% /p:CoverletOutputFormat=\"opencover,json\"

cd $oldPath