# Define the URL and the paths
$url = "http://killzonekid.com/pub/callExtension_v2.0.zip"
$zipPath = "$PSScriptRoot\callExtension_v2.0.zip"
$extractPath = "$PSScriptRoot\callExtension_v2.0"
$finalPath = "$PSScriptRoot\callExtension"

# Download the ZIP file
Invoke-WebRequest -Uri $url -OutFile $zipPath

# Extract the ZIP file
Expand-Archive -Path $zipPath -DestinationPath $extractPath

# Move the callExtension folder to the final path
Move-Item -Path "$extractPath\callExtension" -Destination $finalPath

# Remove the intermediate folder and ZIP file
Remove-Item -Path $extractPath -Recurse -Force
Remove-Item -Path $zipPath -Force

Write-Output "Download and extraction complete. The callExtension folder is ready at $finalPath."

Write-Output "Removing unnecessary files..."
# Remove specific files from the callExtension folder
$filesToRemove = @("readme.txt", "callExtension.exe", "test_callback.dll", "test_callback_x64.dll", "test_extension.dll", "test_extension_x64.dll", "test_script.sqf", "test_script2.sqf", "test_script3.sqf")
foreach ($file in $filesToRemove) {
    $filePath = Join-Path -Path $finalPath -ChildPath $file
    if (Test-Path -Path $filePath) {
        Remove-Item -Path $filePath -Force
    }
}