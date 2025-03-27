# Cross-platform PowerShell script to download and extract FFmpeg and yt-dlp binaries

param (
    [string]$Platform,
    [switch]$LocalBuild
)

# Detect local platform if LocalBuild is enabled
if ($LocalBuild) {
    if ($IsWindows) {
        if ([System.Environment]::Is64BitOperatingSystem) {
            $Platform = "win-x64"
        } else {
            $Platform = "win-x86"
        }
    } elseif ($IsLinux) {
        $Arch = $(uname -m)
        if ($Arch -eq "x86_64") {
            $Platform = "linux-x64" # Also known as amd64, name used by docker.
        } elseif ($Arch -eq "aarch64") {
            $Platform = "linux-arm64"
        } elseif ($Arch -like "armv*") { # All arm (32 bits) variants
            $Platform = "linux-arm"
        } else {
            Write-Host "Unsupported architecture: $Arch. Exiting."
            exit 1
        }
    }
    Write-Host "Detected local platform: $Platform"
}

# Define download URLs based on platform
$FFmpegURL = ""
$YtDlpURL = ""

# Ensure a valid OS and architecture was found
if ($Platform) {
    if ($IsLinux) {
        if ($Platform -eq "linux-x64") { # Also known as amd64
            $FFmpegURL = "https://github.com/yt-dlp/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-linux64-gpl.tar.xz"
            $YtDlpURL = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_linux"
        } elseif ($Platform -eq "linux-arm64") {
            $FFmpegURL = "https://github.com/yt-dlp/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-linuxarm64-gpl.tar.xz"
            $YtDlpURL = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_linux_aarch64"
        } elseif ($Platform -eq "linux-arm") {
            $YtDlpURL = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_linux_armv7l"
        }
    } elseif ($IsWindows) {
        if ($Platform -eq "win-x64") {
            $FFmpegURL = "https://github.com/yt-dlp/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip"
            $YtDlpURL = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe"
        } elseif ($Platform -eq "win-x86") {
            $FFmpegURL = "https://github.com/yt-dlp/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win32-gpl.zip"
            $YtDlpURL = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_x86.exe"
        }
    }

    $OutputDir = "./build/$Platform"
    New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

    # Download and extract FFmpeg
    if ($FFmpegURL -ne "") {
        Write-Host "Downloading FFmpeg..."
        $FFmpegExtractPath = "$OutputDir/FFmpegExtract"
        $ArchivePath = "$OutputDir/FFmpegArchive"
        Invoke-WebRequest -Uri $FFmpegURL -OutFile "$ArchivePath"

        if ($IsWindows) {
            Expand-Archive -Path "$ArchivePath" -DestinationPath "$FFmpegExtractPath"
            $FFmpegBin = Get-ChildItem -Path "$FFmpegExtractPath" -Recurse -Filter "bin" -Directory | Select-Object -First 1
            Move-Item -Path "$FFmpegBin\ffmpeg.exe" -Destination "$OutputDir\ffmpeg.exe"
            Move-Item -Path "$FFmpegBin\ffprobe.exe" -Destination "$OutputDir\ffprobe.exe"
        } else {
            tar -xf "$ArchivePath" --wildcards --no-anchored --strip-components=2 -C "$OutputDir" 'bin/ffmpeg' 'bin/ffprobe'
        }
        
        # Cleanup extracted files
        Remove-Item -Path "$FFmpegExtractPath" -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item -Path "$ArchivePath" -Force -ErrorAction SilentlyContinue
    } else {
        Write-Host "No FFmpeg build available for current OS and architecture combination."
    }

    # Download yt-dlp
    if ($YtDlpURL -ne "") {
        Write-Host "Downloading yt-dlp..."
        $YtDlpPath = "$OutputDir/yt-dlp"
        if ($IsWindows) { $YtDlpPath += ".exe" }
        Invoke-WebRequest -Uri $YtDlpURL -OutFile "$YtDlpPath"
    } else {
        Write-Host "No yt-dlp build available for current OS and architecture combination."
    }

    Write-Host "Download and extraction complete."
} else {
    Write-Warning "Invalid Platform, if running locally please use argument -Localbuild, else define -Platform to the github matrix"
}