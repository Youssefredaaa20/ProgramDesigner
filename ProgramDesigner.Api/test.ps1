$process = Start-Process dotnet -ArgumentList "run" -NoNewWindow -PassThru
Start-Sleep -Seconds 10

$body = @{
    key = "root"
    name = "My Program"
    nodeType = "Group"
    rule = "InOrder"
    choiceCount = 0
    children = @(
        @{
            key = "step1"
            name = "First Step"
            nodeType = "Step"
            stepType = "AttendSession"
        }
    )
} | ConvertTo-Json -Depth 5

Write-Host "Posting to /programs..."
$postResponse = Invoke-RestMethod -Uri "http://localhost:5169/programs" -Method Post -Body $body -ContentType "application/json"
$id = $postResponse.id
Write-Host "Created program ID: $id"

Write-Host "Getting /programs/$id..."
$getResponse = Invoke-RestMethod -Uri "http://localhost:5169/programs/$id" -Method Get
$getResponse | ConvertTo-Json -Depth 5

Write-Host "Stopping API process..."
Stop-Process -Id $process.Id -Force
