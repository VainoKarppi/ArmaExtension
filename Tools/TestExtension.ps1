# Define the paths
$projectPath = Resolve-Path "$PSScriptRoot\.."
Write-Host "Project Path: $projectPath" -ForegroundColor Blue
Write-Host ""


# Define the path to the callExtension folder
$callExtensionPath = "$PSScriptRoot\callExtension"

$existingProcess = Get-Process -Name "callExtension_x64" -ErrorAction SilentlyContinue
if ($existingProcess) {
    Write-Host "Terminating existing callExtension_x64.exe process..." -ForegroundColor Yellow
    Stop-Process -Id $existingProcess.Id -Force
    Start-Sleep -Seconds 1
}

# Check if the callExtension folder exists
if (-Not (Test-Path -Path $callExtensionPath)) {
    Write-Host "callExtension folder not found. Running DownloadExtensionTester.ps1..." -ForegroundColor Green
    
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

    Write-Host "Download and extraction complete. The callExtension folder is ready at $finalPath." -ForegroundColor Blue

    Write-Host "Removing unnecessary files..." -ForegroundColor Blue
    # Remove specific files from the callExtension folder
    $filesToRemove = @("readme.txt", "callExtension.exe", "test_callback.dll", "test_callback_x64.dll", "test_extension.dll", "test_extension_x64.dll", "test_script.sqf", "test_script2.sqf", "test_script3.sqf")
    foreach ($file in $filesToRemove) {
        $filePath = Join-Path -Path $finalPath -ChildPath $file
        if (Test-Path -Path $filePath) {
            Remove-Item -Path $filePath -Force
        }
    }

    Write-Host "callExtension Downloaded Succesfully!" -ForegroundColor Green
} else { 
    Write-Host "CallExtension already installed..." -ForegroundColor Green
}

$destinationPath = "$PSScriptRoot\callExtension"
$hashFilePath = "$destinationPath\files_hash.txt"

# Find the first .csproj file in the project path
$csprojFile = Get-ChildItem -Path $projectPath -Filter *.csproj | Select-Object -First 1

if ($csprojFile) {
    # Read the data from the found .csproj file to get the target DLL name
    [xml]$csproj = Get-Content $csprojFile.FullName

    # Initialize $targetDll as an empty string
    $targetDll = ""
    $assemblyName = ""
    $targetFramework = ""
    $runtimeIdentifier = ""
    

    # Iterate through all PropertyGroup elements to find AssemblyName
    foreach ($propertyGroup in $csproj.Project.PropertyGroup) {
        if ($propertyGroup.AssemblyName) {
            $assemblyName = $propertyGroup.AssemblyName
            $targetDll = $assemblyName + ".dll"
        }
        if ($propertyGroup.TargetFramework) {
            $targetFramework = $propertyGroup.TargetFramework
        }
        if ($propertyGroup.RuntimeIdentifier) {
            $runtimeIdentifier = $propertyGroup.RuntimeIdentifier
        }

        # Stop if all values are found
        if ($assemblyName -ne "" -and $targetFramework -ne "" -and $runtimeIdentifier -ne "") {
            break
        }
    }

    $outputPath = "$projectPath\bin\Release\$targetFramework\$runtimeIdentifier\publish\"

    if ($targetDll) {
        $sourceDllPath = "$outputPath\$targetDll"
        $destinationDllPath = "$destinationPath\$targetDll"

        # Check if the DLL file exists in the destination folder and compare it with the source DLL
        $buildRequired = $true
        $currentHashes = @{}

        # Initialize a variable to store the concatenated hashes
        $combinedHashes = ""

        # Get all .cs files in the project folder and its subfolders
        $csFiles = Get-ChildItem -Path $projectPath -Recurse -Filter *.cs

        # Loop through each .cs file and add its hash to the combined string
        foreach ($file in $csFiles) {
            $hash = Get-FileHash -Path $file.FullName
            $combinedHashes += $hash.Hash  # Concatenate the individual hashes
        }

        # Create a single hash from the concatenated string of hashes
        $finalHash = Get-FileHash -InputStream ([System.IO.MemoryStream]::new([System.Text.Encoding]::UTF8.GetBytes($combinedHashes)))

        # Check if the hash file exists and compare the current hash with the previous one
        if (Test-Path -Path $hashFilePath) {
            $previousHash = Get-Content -Path $hashFilePath

            if ($previousHash -eq $finalHash.Hash) {
                Write-Host "No changes detected in the .cs files. Skipping build..." -ForegroundColor Blue
                $buildRequired = $false
            } else {
                Write-Host "Changes detected in the .cs files. Proceeding with the build..." -ForegroundColor Blue
            }
        }

        if ($buildRequired) {
            # Build the .NET project
            Write-Host "Building the project..." -ForegroundColor Blue
            $buildProcess = Start-Process -FilePath "dotnet" -ArgumentList "publish", $projectPath -WindowStyle Normal -PassThru
            $buildProcess.WaitForExit()

            Write-Host "Building task completed..." -ForegroundColor Blue
            if ($buildProcess.ExitCode -eq 0) {
                Write-Host "Build successful." -ForegroundColor Green
                Write-Host "File Location Folder: $(if (Test-Path $outputPath) { Resolve-Path $outputPath } else { $outputPath })" -ForegroundColor Green


                # Copy the .dll file to the destination folder
                Copy-Item -Path $sourceDllPath -Destination $destinationPath -Force

                if ($?) {
                    Write-Host "Build complete and .dll file copied to $destinationPath." -ForegroundColor Green

                    # Save the current combined hashes to the hash file
                    $finalHash.Hash | Set-Content -Path $hashFilePath
                } else {
                    Write-Host "Failed to copy the .dll file to $destinationPath." -ForegroundColor Red
                }
            } else {
                Write-Host "Build failed." -ForegroundColor Red
                return
            }
        }

        # Copy and Modify test.sqf file
        Write-Host "Copying the tester script..." -ForegroundColor Blue
        $testScriptPath = "$destinationPath\current_test.sqf"
        Copy-Item -Path "$PSScriptRoot\base_test.sqf" -Destination "$testScriptPath" -Force

        if (Test-Path -Path $testScriptPath) {
            $assemblyNameWithoutSuffix = $assemblyName -replace "_x64$", ""
            (Get-Content $testScriptPath) -replace "XXXX", $assemblyNameWithoutSuffix | Set-Content $testScriptPath
            Write-Host "Succesfully Created a copy of the test .SQF Script" -ForegroundColor Green

            # Ensure the file is not in use
            Start-Sleep -Seconds 1

            # Check if the file can be opened for reading
            $fileReady = $false
            for ($i = 0; $i -lt 10; $i++) {
                try {
                    $fileStream = [System.IO.File]::Open($testScriptPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::None)
                    $fileStream.Close()
                    $fileReady = $true
                    break
                } catch {
                    Start-Sleep -Milliseconds 500
                }
            }

            if ($fileReady) {
                # Set the working directory to the destination path
                Set-Location -Path $destinationPath

                # Run callExtension_x64.exe using test.sqf as an argument
                $exePath = "$destinationPath\callExtension_x64.exe"

                # Check if the process is already running and terminate it
                $existingProcess = Get-Process -Name "callExtension_x64" -ErrorAction SilentlyContinue
                if ($existingProcess) {
                    Write-Host "Terminating existing callExtension_x64.exe process..." -ForegroundColor Yellow
                    Stop-Process -Id $existingProcess.Id -Force
                    Start-Sleep -Seconds 1
                }

                if (Test-Path -Path $exePath) {
                    Write-Host "Running callExtension_x64.exe with current_test.sqf..." -ForegroundColor Blue
                    Start-Sleep -Milliseconds 50
                    Start-Process -FilePath $exePath -ArgumentList $testScriptPath
                    Start-Sleep -Seconds 2
                    cd $PSScriptRoot

                    Write-Host ""
                    Write-Host ""
                    Write-Host "=====================" -ForegroundColor Green -BackgroundColor DarkGray
                    Write-Host "   ALL TESTS DONE!   " -ForegroundColor Green -BackgroundColor DarkGray
                    Write-Host "=====================" -ForegroundColor Green -BackgroundColor DarkGray
                    Write-Host ""
                    Write-Host ""
                } else {
                    Write-Host "Executable not found: $exePath" -ForegroundColor Red
                }
            } else {
                Write-Host "File is still in use: $testScriptPath" -ForegroundColor Red
            }
        } else {
            Write-Host "base_test.sqf not found: $testScriptPath" -ForegroundColor Red
        }
    } else {
        Write-Host "AssemblyName not found in the .csproj file." -ForegroundColor Red
    }
} else {
    Write-Host "No .csproj file found in the project path." -ForegroundColor Red
}
