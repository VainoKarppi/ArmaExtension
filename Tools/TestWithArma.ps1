# Import the function script
$functionsScript = "$PSScriptRoot\Functions.ps1"
try {
    . $functionsScript
    Write-Host "Functions loaded successfully." -ForegroundColor Green
} catch {
    Write-Host "Error loading functions: $_" -ForegroundColor Red
    exit 1
}







$projectPath = Get-ProjectPath
$modFolder = "$projectPath\Examples\@$((Get-BuildInfo).AssemblyName -replace '_x64$', '')"


Write-Host "Project Path: $projectPath" -ForegroundColor Blue
Write-Host "Mod Path: $modFolder" -ForegroundColor Blue
Write-Host ""


Terminate-ExistingProcess

if (Build-Project -projectPath $projectPath -destinationPath $modFolder) {
    if (Pack-Addons -modFolder $modFolder) {
        if (Start-Arma) {
            Watch-ArmaLog
        }
    }
}