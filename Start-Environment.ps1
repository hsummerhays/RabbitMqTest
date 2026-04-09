# Start-Environment.ps1
# This script launches Windows Terminal with multiple panes.
# It starts RabbitMQ via Docker, three consumers, and the producer.

$dir = (Get-Location).Path

Write-Host "Building the solution first to avoid concurrent build file-locking..."
dotnet build

# Launch Windows Terminal (wt.exe) with custom panes
# Wait a few seconds for Consumers/Producer to ensure RabbitMQ container starts
wt -d $dir PowerShell -NoExit -Command "Write-Host 'Starting Producer...' \; Start-Sleep -Seconds 5 \; dotnet run --no-build --project Producer/Producer.csproj" `; `
split-pane -H -s 0.3 -d $dir PowerShell -NoExit -Command "Write-Host 'Starting RabbitMQ...' \; docker rm -f some-rabbit 2>&1 | Out-Null \; docker run --name some-rabbit -p 5672:5672 -p 15672:15672 rabbitmq:3-management" `; `
move-focus up `; `
split-pane -V -d $dir PowerShell -NoExit -Command "Write-Host 'Starting Consumer 1...' \; Start-Sleep -Seconds 5 \; dotnet run --no-build --project Consumer1/Consumer1.csproj" `; `
split-pane -H -d $dir PowerShell -NoExit -Command "Write-Host 'Starting Consumer 2...' \; Start-Sleep -Seconds 5 \; dotnet run --no-build --project Consumer2/Consumer2.csproj" `; `
split-pane -H -d $dir PowerShell -NoExit -Command "Write-Host 'Starting Consumer 3...' \; Start-Sleep -Seconds 5 \; dotnet run --no-build --project Consumer3/Consumer3.csproj"
