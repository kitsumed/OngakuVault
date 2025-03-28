# This script should be run on Linux for 100% support. Its can also be run on Windows for windows builds, but has a very VERY limited support when handling linux builds.

param (
    [string]$artifactDir,
    [string]$outputDir,
    [switch]$compressOutput
)

# Check if the artifactDir exists
if (-not (Test-Path -Path $artifactDir)) {
    Write-Warning "The artifact directory '$artifactDir' does not exist."
    exit 1
}

# Ensure the outputDir exists
New-Item -Path $outputDir -ItemType Directory -ErrorAction SilentlyContinue

# Iterate over the artifacts and process them
$artifacts = Get-ChildItem -Path $artifactDir -File
foreach ($artifactFile in $artifacts) {
    Write-Host "Processing artifact: $artifactFile"

    # If the artifact is a ZIP archive (should always be)
    if ($artifactFile.Extension -eq ".zip") {
        # If the artifact name starts with "win-", do not extract, just calculate the hash
        if ($artifactFile.Name -like "win-*") {
            Write-Host "Detected $artifactFile as a Windows build."
            # If compress output flag is enabled, keep current zip archive, else unzip
            if ($compressOutput) {
                # Move win build to output directory
                Move-Item -Path $artifactFile -Destination $outputDir
            } else {
                # Extract zip artifact to the outpoutDir
                $extractDir = "$outputDir\$($artifactFile.BaseName)"
                Write-Host "Extracting $artifactFile to $extractDir"
                Expand-Archive -Path $artifactFile.FullName -DestinationPath $extractDir
                # Remove the zip artifact archive
                Write-Host "Cleaning up zip artifact archive : $artifactFile"
                Remove-Item -Path "$artifactFile" -Recurse -ErrorAction SilentlyContinue
            }
        }
        # If the artifact name starts with "linux-", extract, apply chmod, recompress (tar.gz), and calculate hash
        elseif ($artifactFile.Name -like "linux-*") {
            Write-Host "Detected $artifactFile as a Linux build."
            # Extract zip artifact
            $extractDir = "$artifactDir\$($artifactFile.BaseName)"
            Write-Host "Extracting $artifactFile to $extractDir"
            Expand-Archive -Path $artifactFile.FullName -DestinationPath $extractDir

            if ($IsLinux) {
                # The name of the binaries to apply chmod to
                $binariesToChmod = @("yt-dlp", "ffmpeg", "ffprobe")
                foreach ($binaryName in $binariesToChmod) {
                    $binaryPath = Join-Path -Path $extractDir -ChildPath $binaryName
                    if (Test-Path -Path $binaryPath) {
                        Write-Host "Adding read and execution permission to $binaryName"
                        chmod a+rx $binaryPath
                    } else {
                        Write-Warning "Could not chmod $binaryName, binary not found."
                    }
                }
            } else {
                Write-Warning("Could not apply chmod to binary as the script is running on a non-linux system.")
            }


            if ($compressOutput) {
                # Recompress the directory into a tar.gz archive in the outputDir (preserving chmod)
                $tarFilePath = "$outputDir\$($artifactFile.BaseName).tar.gz"
                Write-Host "Recompressing the extracted files into $tarFilePath"
                # Create a tar.gz archive
                tar -czf "$tarFilePath" -C "$extractDir" *
                # Check if the command failed (prevent deleting original files from $artifactDir)
                if (!$?) {
                    Write-Warning "An error occurred while creating the tar.gz archive. Stopping execution..."
                    exit 1
                }
                # Remove the zip artifact archive and it's decompression
                Write-Host "Cleaning up zip artifact archive : $artifactFile"
                Remove-Item -Path "$artifactFile" -Recurse -ErrorAction SilentlyContinue
                Write-Host "Cleaning up decompressed archive : $extractDir"
                Remove-Item -Path "$extractDir" -Recurse -ErrorAction SilentlyContinue
            }
            else {
                # Move the extracted folder to the output directory
                Move-Item -Path $extractDir -Destination $outputDir
            }
        }
    }
}

Write-Host "Artifact processing complete."