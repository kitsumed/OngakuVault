# This script create / append the current versions of external binaries (taken from github api) to a .md file

param (
    [string]$mdFileOut
)

if ($mdFileOut.Length -eq 0) {
    Write-Warning "mdFilePath param is missing."
    Exit 1
} # Ensure the directory exists
elseif (!(Test-Path -Path $mdFileOut -PathType Leaf)) {
    # Ensure the markdown file exists
    New-Item -ItemType File -Path $mdFileOut
} 

# Invoke-RestMethod parses json by default
$ytdlpJsonResponse = Invoke-RestMethod -Method Get -Uri "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest"
$ffmpegJsonResponse = Invoke-RestMethod -Method Get -Uri "https://api.github.com/repos/yt-dlp/FFmpeg-Builds/releases/latest"
# $ytdlpJsonResponse | Get-Member

Add-Content -Path $mdFileOut -Value "`n`nThis build **may contain external binaries** at the following versions:"
Add-Content -Path $mdFileOut -Value "`n- **[yt-dlp]($($ytdlpJsonResponse.html_url))**: ``$($ytdlpJsonResponse.tag_name)``"
Add-Content -Path $mdFileOut -Value "- **[FFmpeg]($($ffmpegJsonResponse.html_url))**: ``$($ffmpegJsonResponse.name)``"