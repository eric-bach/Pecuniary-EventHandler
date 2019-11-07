[CmdletBinding()]
param (
    [Parameter(Mandatory=$false, HelpMessage="Enter your config name defined in developers.json: ")][string]$configName
)

$environment = Get-Content 'developers.json' | Out-String | ConvertFrom-Json
$config = $environment.$configName
$developerPrefix = $config.Prefix

$stackName = "pecuniary-eventhandler-stack"

$sourceFile = "samTemplate.yaml"
$localSourceFile = "$sourceFile.local"
Write-Host "`nCreating/updating $localSourceFile based on $sourceFile..."
Copy-Item samTemplate.yaml $localSourceFile

if ($config.Prefix)
{  
    Write-Host "`n`tDeveloper config selected" -ForegroundColor Yellow
    Write-Host "`Parameters from " -NoNewline
    Write-Host "developers.json:`n" -ForegroundColor Cyan
    Write-Host "`tdeveloperPrefix: `t`t $developerPrefix" -ForegroundColor Yellow

    $stackName = $developerPrefix + "-" + $stackName

    (Get-Content $localSourceFile) `
        -replace 'pecuniary-', "$developerPrefix-pecuniary-" `
        -replace 'Name: pecuniary', "Name: $developerPrefix-pecuniary" |
    Out-File $localSourceFile -Encoding utf8

    Write-Host "`nDone! $localSourceFile updated. Please use this file when deploying to our own AWS stack.`n"

    Write-Host "Press [enter] to continue deploying stack to AWS (Ctrl+C to exit)" -NoNewline -ForegroundColor Green
    Read-Host

    Write-Host "`n`nRestoring projects..." -ForegroundColor Cyan

    dotnet restore

    Write-Host "`n`nBuilding projects..." -ForegroundColor Cyan

    dotnet publish -c Release
}

Write-Host "`n`nDeploying stack $stackName..." -ForegroundColor Cyan

dotnet-lambda deploy-serverless `
    --stack-name $stackName `
    --template $localSourceFile `
    --region us-west-2 `
    --s3-bucket pecuniary-deployment-artifacts

# Handle deploy errors
if ($lastexitcode -ne 0) {
    throw "Error deploying" + $stackName
}

Write-Host "`n`n YOUR STACK NAME IS:   " -NoNewLine
Write-Host "$stackName   `n`n" -ForegroundColor Cyan
