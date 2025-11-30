# Replacing third party binary
If you're using a build of Ongakuvault that is several months old or that exclude some binary, you might consider manually updating the third-party binary.
As long as the yt-dlp binary arguments haven't changed significantly, Ongakuvault should be compatible with more recent versions.

## Installation
Ongakuvault may include the following third-patry binary:

- [yt-dlp](https://github.com/yt-dlp/yt-dlp/releases/latest)
- [yt-dlp version of FFmpeg & FFprobe](https://github.com/yt-dlp/FFmpeg-Builds/releases/latest)
- [Deno](https://github.com/denoland/deno/releases)

> [!TIP]
> You can find the PowerShell script used during the build & release workflow to download the external binaries [here](https://github.com/kitsumed/OngakuVault/blob/main/.github/workflows/scripts/download_external_binaries.ps1).
> You can run it locally on your machine by using the argument `-LocalBuild`. This will detect your system architecture and download the latest third-party binaries available.

## Replacing a binary
### General
If you downloaded Ongakuvault from GitHub releases or built it yourself, you need to place all third-party binaries in the same directory as the executable. (Where ``Ongakuvault.exe`` / ``Ongakuvault.dll`` is)
### For docker images

| Third party binary | Path in docker container |
|--------------------|--------------------------|
| yt-dlp             | /app/yt-dlp              |
| FFmpeg             | /app/ffmpeg              |
| FFprobe            | /app/ffprobe             |
| Deno               | /app/deno                |

1. Run ``chmod a+rx file_name`` on every new binary
2. On your docker configuration, add a read-only binding to the new binary

::: details Docker run example
```bash{10}
docker run -d \
  --name ongakuvault \
  --restart unless-stopped \
  --log-driver local \
  --log-opt max-file=10 \
  --log-opt max-size=5m \
  --cap-drop all \
  --security-opt=no-new-privileges \
  -v /my-audio-collection-directory-on-host:/home/app/archived-audios:rw \
  -v /path/to/host/ffmpeg:/app/ffmpeg:ro \ # Overwrite container /app/ffmpeg for host /path/to/host/ffmpeg in read-only (:ro) mode
  -e ASPNETCORE_HTTP_PORTS=8080 \
  -e ASPNETCORE_HTTPS_PORT=8443 \
  -e Ongaku__ENFORCE_HTTPS=false \
  ghcr.io/kitsumed/ongakuvault:1.0.0
```
:::

::: details Docker-compose example
```yaml{25-29}
version: '2.4'

services:
  ongakuvault:
    container_name: OngakuVault
    image: ghcr.io/kitsumed/ongakuvault:1.0.0 # Replace tag with the version you want / latest version
    restart: unless-stopped
    logging:
      driver: local
      options:
        max-file: '10'
        max-size: '5m'
    cap_drop:
      - ALL
    security_opt:
      - "no-new-privileges=true"
    ports:
      - "8080:8080"  # Expose port host:container on interface 0.0.0.0 (all host interfaces)
      - "8443:8443"  # Expose port host:container on interface 0.0.0.0 (all host interfaces)
    volumes:
      - type: bind  # Redirect app output to a folder on your host system
        source: /my-audio-collection-directory-on-host
        target: /home/app/archived-audios/ # This is the default final output folder of Ongakuvault when running in a container
        read_only: false
    volumes:
      - type: bind  # Overwrite container /app/ffmpeg for host /path/to/host/ffmpeg in read-only mode
        source: /path/to/host/ffmpeg
        target: /app/ffmpeg 
        read_only: true
    environment:
      - ASPNETCORE_HTTP_PORTS=8080
      - ASPNETCORE_HTTPS_PORT=8443
      - Ongaku__ENFORCE_HTTPS=false
```
:::