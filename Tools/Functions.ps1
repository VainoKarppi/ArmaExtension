function Get-ProjectPath {
    return Resolve-Path "$PSScriptRoot\.."
}

function Terminate-ExistingProcess {
    $existingTesterProcess = Get-Process -Name "callExtension_x64" -ErrorAction SilentlyContinue
    if ($existingTesterProcess) {
        Write-Host "Terminating existing callExtension_x64.exe process..." -ForegroundColor Yellow
        Stop-Process -Id $existingTesterProcess.Id -Force
        Start-Sleep -Seconds 1
    }

    $existingArmaProcess = Get-Process -Name "arma3_x64" -ErrorAction SilentlyContinue
    if ($existingArmaProcess) {
        Write-Host "Terminating existing arma3_x64.exe process..." -ForegroundColor Yellow
        Stop-Process -Id $existingArmaProcess.Id -Force
        Start-Sleep -Seconds 1
    }
}

function Download-CallExtension {
    $callExtensionPath = "$PSScriptRoot\callExtension"
    if (-Not (Test-Path -Path $callExtensionPath)) {
        Write-Host "callExtension folder not found. Downloading..." -ForegroundColor Yellow
        
        $url = "http://killzonekid.com/pub/callExtension_v2.0.zip"
        $zipPath = "$PSScriptRoot\callExtension_v2.0.zip"
        $extractPath = "$PSScriptRoot\callExtension_v2.0"
        $finalPath = "$PSScriptRoot\callExtension"
        
        Invoke-WebRequest -Uri $url -OutFile $zipPath
        Expand-Archive -Path $zipPath -DestinationPath $extractPath
        Move-Item -Path "$extractPath\callExtension" -Destination $finalPath
        Remove-Item -Path $extractPath -Recurse -Force
        Remove-Item -Path $zipPath -Force

        Write-Host "Removing unnecessary files..." -ForegroundColor Blue
        # Remove specific files from the callExtension folder
        $filesToRemove = @("readme.txt", "callExtension.exe", "test_callback.dll", "test_callback_x64.dll", "test_extension.dll", "test_extension_x64.dll", "test_script.sqf", "test_script2.sqf", "test_script3.sqf")
        foreach ($file in $filesToRemove) {
            $filePath = Join-Path -Path $finalPath -ChildPath $file
            if (Test-Path -Path $filePath) {
                Remove-Item -Path $filePath -Force
            }
        }
        
        Write-Host "callExtension Downloaded Successfully!" -ForegroundColor Green
    } else {
        Write-Host "CallExtension already installed..." -ForegroundColor Green
    }
}

function Get-BuildInfo {
    $projectPath = Get-ProjectPath
    
    $csprojFile = Get-ChildItem -Path $projectPath -Filter *.csproj | Select-Object -First 1
    if (-not $csprojFile) {
        Write-Host "No .csproj file found from path: $projectPath" -ForegroundColor Red
        return $null
    }
    
    [xml]$csproj = Get-Content $csprojFile.FullName
    $assemblyName = ($csproj.Project.PropertyGroup | Where-Object { $_.AssemblyName }).AssemblyName
    $version = ($csproj.Project.PropertyGroup | Where-Object { $_.Version }).Version
    $targetFramework = ($csproj.Project.PropertyGroup | Where-Object { $_.TargetFramework }).TargetFramework
    $runtimeIdentifier = ($csproj.Project.PropertyGroup | Where-Object { $_.RuntimeIdentifier }).RuntimeIdentifier
    
    if ($assemblyName -and $targetFramework -and $runtimeIdentifier) {
        return @{ AssemblyName = $assemblyName; Version = $version; TargetFramework = $targetFramework; RuntimeIdentifier = $runtimeIdentifier }
    }
    
    Write-Host "AssemblyName not found in the .csproj file." -ForegroundColor Red
    return $null
}

function Compute-ProjectHash {
    param ($projectPath)
    
    $combinedHashes = ""
    $csFiles = Get-ChildItem -Path $projectPath -Recurse -Filter *.cs
    foreach ($file in $csFiles) {
        $hash = Get-FileHash -Path $file.FullName
        $combinedHashes += $hash.Hash
    }
    return Get-FileHash -InputStream ([System.IO.MemoryStream]::new([System.Text.Encoding]::UTF8.GetBytes($combinedHashes)))
}

function Build-Project {
    param ($projectPath, $destinationPath)

    $buildInfo = Get-BuildInfo

    $outputPath = "$projectPath\bin\Release\$($buildInfo.TargetFramework)\$($buildInfo.RuntimeIdentifier)\publish"
    $dllName = "$($buildInfo.AssemblyName).dll"
    $hashFilePath = "$destinationPath\hash.txt"
    
    $currentHash = Compute-ProjectHash -projectPath $projectPath
    if (Test-Path -Path $hashFilePath) {
        $previousHash = Get-Content -Path $hashFilePath
        if ($previousHash -eq $currentHash.Hash) {
            Write-Host "No changes detected in the .cs files. Skipping build..." -ForegroundColor Blue
            return $true
        }
    }
    
    Write-Host "Building the project..." -ForegroundColor Blue
    $buildProcess = Start-Process -FilePath "dotnet" -ArgumentList "publish", $projectPath -WindowStyle Normal -PassThru
    $buildProcess.WaitForExit()
    
    if ($buildProcess.ExitCode -eq 0) {
        Write-Host "Build successful." -ForegroundColor Green
        $currentHash.Hash | Set-Content -Path $hashFilePath
        
        $sourceDllPath = "$outputPath\$dllName"
        $destinationDllPath = "$destinationPath\$dllName"

        Copy-Item -Path $sourceDllPath -Destination $destinationDllPath -Force
        Write-Host "Copied $dllName to $destinationPath" -ForegroundColor Green
        
        return $true
    } else {
        Write-Host "Build failed." -ForegroundColor Red
        return $false
    }
}

function Prepare-TestScript {
    param ($destinationPath)

    $buildInfo = Get-BuildInfo
    $assemblyName = $buildInfo.AssemblyName
    
    $testScriptPath = "$destinationPath\current_test.sqf"
    Copy-Item -Path "$PSScriptRoot\base_test.sqf" -Destination "$testScriptPath" -Force
    
    if (Test-Path -Path $testScriptPath) {
        $assemblyNameWithoutSuffix = $assemblyName -replace "_x64$", ""
        (Get-Content $testScriptPath) -replace "XXXX", $assemblyNameWithoutSuffix | Set-Content $testScriptPath
        Write-Host "Succesfully created a copy of the test .SQF script" -ForegroundColor Green

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

        return $fileReady
    } else {
        Write-Host "base_test.sqf not found" -ForegroundColor Red
        return $false
    }
}

function Run-CallExtension {
    param ($destinationPath, $testScriptPath)

    Set-Location -Path $destinationPath
    
    $exePath = "$destinationPath\callExtension_x64.exe"
    if (Test-Path -Path $exePath) {
        Write-Host "Running callExtension_x64.exe..." -ForegroundColor Blue
        Start-Process -FilePath $exePath -ArgumentList $testScriptPath
        Start-Sleep -Seconds 1
    } else {
        Write-Host "Executable not found: $exePath" -ForegroundColor Red
    }

    cd $PSScriptRoot
}

function Pack-Addons {
    param ($modFolder)

    # Path to Addon Builder (change this if using PBOProject)
    $addonBuilderPath = (Get-ItemProperty -Path "HKLM:\SOFTWARE\WOW6432Node\Bohemia Interactive\AddonBuilder" -ErrorAction Stop).path
    Write-Host "Arma Tools Path: $addonBuilderPath"  -ForegroundColor Blue

    # Pack the addon
    if (!(Test-Path $addonBuilderPath)) {
        Write-Host "Addon Builder not found from ($addonBuilderPath). Check the path or use another PBO tool."
        return $false
    }

    # Get all subfolders in addons directory
    $addonFolders = Get-ChildItem "$modFolder\addons" -Directory
    $i = 0
    foreach ($folder in $addonFolders) {
        $sourceAddonFolder = $($folder.FullName)
        $addonFolder = "$modFolder\addons\"

        Write-Host "Packing addon: $sourceAddonFolder" -ForegroundColor Yellow

        & "$addonBuilderPath\AddonBuilder.exe" "$sourceAddonFolder" "$addonFolder" -packonly

        # Verify if the PBO file was created
        # Check process exit code
        $pboFile = "$addonFolder$($folder.Name).pbo"
        if (!(Test-Path $pboFile)) {
            Write-Host "Addon Builder failed with exit code $($process.ExitCode) for $($folder.Name)." -ForegroundColor Red
            exit $false
        }

        Write-Host "Addon packed successfully: $pboFile" -ForegroundColor Green
        $i = $i + 1
    }

    Write-Host "All addons ($i) processed." -ForegroundColor Green

    return $true
}

function Watch-ExtensionLog {
    $armaPath = Get-ArmaPath

    Start-Sleep -Seconds 2

    if (Test-Path -Path "$armaPath") {
        $logFolder = "$armaPath\" + $((Get-BuildInfo).AssemblyName -replace '_x64$', '') + "_Logs"
        if (!(Test-Path -Path $logFolder)) {
            Write-Host "Log folder not found: $logFolder" -ForegroundColor Red
            return
        }

        # Get the latest log file timestamp at the time of execution
        $latestLog = Get-ChildItem -Path $logFolder -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        $initialTime = if ($latestLog) { $latestLog.LastWriteTime } else { Get-Date }

        Write-Host "Waiting for a new log file at ('$logFolder') after: $initialTime" -ForegroundColor Yellow

        # Wait for a new log file to appear
        do {
            Start-Sleep -Seconds 1

            # Check if the Arma 3 process is running
            if (-not (Get-Process -Name "arma3_x64" -ErrorAction SilentlyContinue)) {
                Write-Host "Arma 3 is no longer running." -ForegroundColor Red
                return
            }

            $newLog = Get-ChildItem -Path $logFolder -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        } while (-not $newLog -or $newLog.LastWriteTime -le $initialTime)

        Write-Host "New log file detected: $($newLog.FullName)" -ForegroundColor Green

        # Live monitoring of the new log file with Ctrl+C handling
        try {
            Get-Content -Path $newLog.FullName -Wait | ForEach-Object {
                Write-Host $_
            }
        } catch {
            if ($_.Exception.GetType().Name -eq "OperationCanceledException") {
                Write-Host "Log monitoring was stopped by user (Ctrl+C)." -ForegroundColor Yellow
            } else {
                Write-Host "An error occurred during log monitoring: $_" -ForegroundColor Red
            }
        }

        # Monitor Arma 3 and stop log monitoring if needed
        while ($true) {
            Start-Sleep -Seconds 1

            # Check if Arma 3 is still running
            if (-not (Get-Process -Name "arma3_x64" -ErrorAction SilentlyContinue)) {
                Write-Host "Arma 3 has stopped. Stopping log monitoring." -ForegroundColor Red
                return
            }
        }
    } else {
        Write-Host "Arma 3 executable not found: $armaPath\arma3_x64.exe" -ForegroundColor Red
    }
}

function Watch-RPTLog {
    $armaLogPath = "$env:LOCALAPPDATA\Arma 3"

    Start-Sleep -Seconds 2

    if (Test-Path -Path "$armaLogPath") {
        $latestRPT = Get-ChildItem -Path $armaLogPath -Filter "*.RPT" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        $initialTime = if ($latestRPT) { $latestRPT.LastWriteTime } else { Get-Date }

        Write-Host "Waiting for a new .RPT file at ('$armaLogPath') after: $initialTime" -ForegroundColor Yellow

        # Wait for a new .RPT file to appear
        do {
            Start-Sleep -Seconds 1

            # Check if the Arma 3 process is running
            if (-not (Get-Process -Name "arma3_x64" -ErrorAction SilentlyContinue)) {
                Write-Host "Arma 3 is no longer running." -ForegroundColor Red
                return
            }

            $newRPT = Get-ChildItem -Path $armaLogPath -Filter "*.RPT" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        } while (-not $newRPT -or $newRPT.LastWriteTime -le $initialTime)

        Write-Host "New .RPT file detected: $($newRPT.FullName)" -ForegroundColor Green

        # Live monitoring of the new .RPT file with Ctrl+C handling
        try {
            Get-Content -Path $newRPT.FullName -Wait | ForEach-Object {
                Write-Host $_
            }
        } catch {
            if ($_.Exception.GetType().Name -eq "OperationCanceledException") {
                Write-Host "Log monitoring was stopped by user (Ctrl+C)." -ForegroundColor Yellow
            } else {
                Write-Host "An error occurred during log monitoring: $_" -ForegroundColor Red
            }
        }

        # Monitor Arma 3 and stop log monitoring if needed
        while ($true) {
            Start-Sleep -Seconds 1

            # Check if Arma 3 is still running
            if (-not (Get-Process -Name "arma3_x64" -ErrorAction SilentlyContinue)) {
                Write-Host "Arma 3 has stopped. Stopping log monitoring." -ForegroundColor Red
                return
            }
        }
    } else {
        Write-Host "Arma 3 log folder not found: $armaLogPath" -ForegroundColor Red
    }
}

function Get-ArmaPath {
    $armaPath = (Get-ItemProperty -Path "HKLM:\SOFTWARE\WOW6432Node\Bohemia Interactive\ArmA 3" -ErrorAction Stop).main

    if (-not $armaPath) {
        Write-Host "Arma 3 path not found in registry." -ForegroundColor Red
        return ""
    }

    return $armaPath
}

function Start-Arma {
    $armaPath = Get-ArmaPath

    if (Test-Path -Path $armaPath) {
        Write-Host "Arma 3 Started at: $armaPath" -ForegroundColor Blue

        # Start Arma 3 with the mod in windowed mode
        $armaExe = "$armaPath\arma3_x64.exe"
        $launchArgs = "-window -mod=$modFolder -noBattleEye -nosplash -skipIntro -noPause -enableHT -hugePages"

        Write-Host "Launching Arma 3..." -ForegroundColor Green
        Start-Process -FilePath $armaExe -ArgumentList $launchArgs -NoNewWindow

        return $true
    } else {
        Write-Host "Arma 3 path not found or invalid!: ($armaPath)" -ForegroundColor Red
        return $false
    }
}