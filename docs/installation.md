---
outline: deep
---
# Installation
Ongakuvault can be downloaded from pre-build binary and docker images are also available on the github packages registry.
Additionally, you can build the project yourself. We also strongly recommend reading the [configurations section](./configurations/).

| Specificities                                                                                        | win x32 | win x64 | arm (x32)                           | arm64 | amd64 |
|------------------------------------------------------------------------------------------------------|---------|---------|-------------------------------------|-------|-------|
| [Available as binary](#from-binary-releases)                                                         | ✅      | ✅      | ✅                                | ✅    | ✅    |
| [Available as docker image](#from-docker)                                                            | ❌      | ❌      | ✅                                | ✅    | ✅    |
| All third-party binaries included                                                                    | ✅      | ✅      | ⚠️ (only includes **yt-dlp**)     | ✅    | ✅    |

## Installation Methods
Before installing Ongakuvault, please take note of the following: 
> [!IMPORTANT]
> The binary and Docker release for `linux arm32` (also referred to as `ARM`), only includes the `yt-dlp` third-party library. This is because Ongakuvault
> includes [yt-dlp version of FFmpeg and FFprobe](https://github.com/yt-dlp/FFmpeg-Builds), versions that do not have builds for the `ARM` architecture. If you plan to use Ongakuvault on an `ARM`
> architecture, you need to download a `ARM` version of FFmpeg and FFprobe from a third-party source. Both FFmpeg and FFprobe should be placed in the same
> directory as the Ongakuvault binaries. [Learn more here](./configurations/replacing-third-party-binary). **Note that this does not apply to ARM64 builds.**

> [!TIP]
> You can find all of the application configuration environment variables [here](./configurations/environment-variables).
> Additionally, links to the Microsoft documentation for other configuration methods are also provided.

### From binary releases
You can access the latest binary release [here](https://github.com/kitsumed/OngakuVault/releases/latest).
These releases currently include builds for the following platforms : `win x64`, `win x32`, `linux arm32`, `linux arm64`, `linux amd64`.
> [!NOTE]
> Ongakuvault builds do not include the .NET Core Runtime in their binary to minimize file size. This means you'll need to install it on your system if you haven't done so already.

#### Installing the .NET Core Runtime  
1. Visit the [Microsoft .NET Core 8 download page](https://dotnet.microsoft.com/download/dotnet/8.0).  
2. Choose the latest 8.x.x version available.  
3. Find the section to download the **ASP.NET Core Runtime**.  
4. For Windows users, download the installer matching your system architecture. For Linux users, follow the package manager instructions provided by Microsoft. 


### From Docker
Docker images are available on the github packages registry, you can pull a specific version of Ongakuvault by running `docker image pull ghcr.io/kitsumed/OngakuVault:1.0.0`.

**Make sure to replace the `:1.0.0` tag with the latest version available**, or the specific version you want to download. You can see all docker images along with their tag published on github [here](https://github.com/kitsumed/OngakuVault/pkgs/container/Ongakuvault).

Docker images are currently available for the following platforms : `linux arm32`, `linux arm64`, `linux amd64`.
> [!NOTE]
> As previously mentionned, docker release for `linux arm32` (also referred to as `ARM`), only includes `yt-dlp`. If you plan on using `ARM`, you need to create a read-only binding
> to the binary you want to replace. [Learn more here](./configurations/replacing-third-party-binary)

#### Docker Run
This is a example of a ``docker run`` command.

Not everything here is required, for example everything related to logs can be removed, `--cap_drop` on **ALL** and `security-opt=no-new-privileges` are [recommended for more security](https://www.redhat.com/en/blog/secure-your-containers-one-weird-trick).
```bash
docker run -d \
  --name ongakuvault \
  --restart unless-stopped \
  --log-driver local \
  --log-opt max-file=10 \
  --log-opt max-size=5m \
  --cap-drop all \
  --security-opt=no-new-privileges \
  -v /my-audio-collection-directory-on-host:/home/app/archived-audios:rw \
  -e ASPNETCORE_HTTP_PORTS=8080 \
  -e ASPNETCORE_HTTPS_PORT=8443 \
  -e ENFORCE_HTTPS=false \
  ghcr.io/kitsumed/ongakuvault:1.0.0
```
#### Docker-compose
This is a example of a ``docker-compose`` configuration.

Not everything here is required, for example the `logging` section is totally optional, `cap_drop` on **ALL** and `security-opt=no-new-privileges` are [recommended for more security](https://www.redhat.com/en/blog/secure-your-containers-one-weird-trick).
```yaml
version: '2.4'

services:
  ongakuvault:
    container_name: ongakuvault
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
    environment:
      - ASPNETCORE_HTTP_PORTS=8080
      - ASPNETCORE_HTTPS_PORT=8443
      - ENFORCE_HTTPS=false
```