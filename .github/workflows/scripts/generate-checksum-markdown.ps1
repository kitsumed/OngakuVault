param (
    [string]$dirPath,
    [string]$checksumFileOut
)

# Ensure the directory exists
if (!(Test-Path -Path $dirPath -PathType Container)) {
    Write-Warning "dirPath param is missing."
    exit 1
}

# Ensure the checksum file is writable
try {
    New-Item -ItemType File -Path $checksumFileOut
} catch {
    Write-Warning "Unable to create the checksum file at $checksumFileOut (param checksumFileOut)."
    exit 1
}

# Collect file hashes
$files = Get-ChildItem -Path $dirPath -File

# Table header
$markdown = "`n`n## Checksum table`n"
$markdown += "| File Name | MD5 | SHA1 | SHA256 |`n"
$markdown += "|----------|----|------|-------|`n"

foreach ($file in $files) {
    # Skip checksum.md
    if ($file.Name -eq "checksum.md") {
        continue
    }
    $md5 = (Get-FileHash -Path $file.FullName -Algorithm MD5).Hash
    $sha1 = (Get-FileHash -Path $file.FullName -Algorithm SHA1).Hash
    $sha256 = (Get-FileHash -Path $file.FullName -Algorithm SHA256).Hash
    $markdown += "| ``$($file.Name)`` | ``$md5`` | ``$sha1`` | ``$sha256`` |`n"
}

# Write to the checksum file
$markdown | Set-Content -Path $checksumFileOut
Write-Host "Checksum file generated at: $checksumFileOut"