
$oldPath = $PWD.Path
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$benchmarkPath = Join-Path $scriptPath "..\EvenireDB.Benchmark\"
cd $benchmarkPath
dotnet restore
dotnet build -c Release
cd 'bin\Release\net7.0\'
dotnet EvenireDB.Benchmark.dll
cd $oldPath

