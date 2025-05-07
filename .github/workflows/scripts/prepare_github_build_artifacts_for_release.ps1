# This script should be run on Linux for 100% support. Its can also be run on Windows for windows builds, but has a very VERY limited support when handling linux builds.

param (
    [string]$artifactDir,
    [string]$outputDir,
    [switch]$compressOutput
)

# Check if the artifactDir does not exists
if (!(Test-Path -Path $artifactDir)) {
    Write-Warning "The artifact directory '$artifactDir' does not exist."
    exit 1
}

# Ensure the outputDir exists
New-Item -Path $outputDir -ItemType Directory -ErrorAction SilentlyContinue

# Iterate over the artifacts and process them
$artifacts = Get-ChildItem -Path $artifactDir -Directory
foreach ($artifactFolder in $artifacts) {
    Write-Host "Processing artifact: $artifactFolder"


    # If the artifact is a windows build
    if ($artifactFolder.Name -like "win-*") {
        Write-Host "Detected $artifactFolder as a Windows build."
        # If compress output flag is enabled, compress to output, else move directory to output
        if ($compressOutput) {
            # Compress files to output
            $zipFilePath = "$outputDir\$($artifactFolder.Name).zip"
            Write-Host "Compressing $artifactFolder to $zipFilePath"
            Compress-Archive -Path "$($artifactFolder.FullName)\*" -DestinationPath $zipFilePath
            Write-Host "Cleaning up artifact archive : $artifactFolder"
            Remove-Item -Path "$artifactFolder" -Recurse -ErrorAction SilentlyContinue
        }
        else {
            Move-Item -Path $artifactFolder.FullName -Destination $outputDir
        }
    }
    # If the artifact name starts with "linux-", apply chmod, recompress (tar.gz) or move
    elseif ($artifactFolder.Name -like "linux-*") {
        Write-Host "Detected $artifactFolder as a Linux build."
        if ($IsLinux) {
            # The name of the binaries to apply chmod to
            $binariesToChmod = @("yt-dlp", "ffmpeg", "ffprobe")
            foreach ($binaryName in $binariesToChmod) {
                $binaryPath = Join-Path -Path $artifactFolder.FullName -ChildPath $binaryName
                if (Test-Path -Path $binaryPath) {
                    Write-Host "Adding read and execution permission to $binaryName"
                    chmod a+rx $binaryPath
                }
                else {
                    Write-Warning "Could not chmod $binaryName, binary not found."
                }
            }
        }
        else {
            Write-Warning("Could not apply chmod to binary as the script is running on a non-linux system.")
        }


        if ($compressOutput) {
            # Path of where the compressed tar.gz archive (preserve chmod) will be created, including file name
            $tarFilePath = "$outputDir/$($artifactFolder.Name).tar.gz"
            Write-Host "Recompressing the extracted files into $tarFilePath"
            # Create a tar.gz archive
            # NOTE: For some obscure reasons, we need to have "." here, causing the root folder to be a ".". Multiples source says "*" can work, and it does on Windows, but keep failing on linux as it try to go into the parent directory
            tar cvzf "$tarFilePath" -C "$($artifactFolder.FullName)" .
            # Check if the previous command failed (prevent deleting original files from $artifactDir)
            if (!$?) {
                Write-Warning "An error occurred while creating the tar.gz archive from: $($artifactFolder.FullName) to: $tarFilePath. Stopping execution..."
                exit 1
            }
            # Remove the artifact archive
            Write-Host "Cleaning up artifact archive : $artifactFolder"
            Remove-Item -Path "$artifactFolder" -Recurse -ErrorAction SilentlyContinue
        }
        else {
            # Move the artifact folder to the output directory
            Move-Item -Path $artifactFolder.FullName -Destination $outputDir
        }
    }
}

Write-Host "Artifact processing complete."