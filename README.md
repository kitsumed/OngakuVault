# What is OngakuVault?
Ongakuvault is an ASP.NET API running on .NET Core 8, designed to work on both Windows and Linux. It allows users to download audio files, retain their metadata, and make modifications to selected fields for better management
of your audio archive. The application also includes an optional web interface (enabled by default), meaning no installation is required for end-users! Additionally, Docker images are available for easy deployment.
[**Check out Ongakuvault documentation**](https://kitsumed.github.io/ongakuvault/).

# Features
- Fetch original media information from a direct URL or website.
- Ability to overwrite metadata fields for better customization.
- Supports a wide range of websites via yt-dlp integration.
- Automates the conversion of video with audio to audio-only formats.
- Recommend a lossless codec (format) when lossless audio is detected when fetching media information.
- Keep original file metadata by default, including embedded thumbnail / artist cover.

# [Showcase](https://kitsumed.github.io/ongakuvault/getting-started.html#showcase)
You can see a showcase of the desktop & mobile interface along with a video of the web interface [here]((https://kitsumed.github.io/ongakuvault/getting-started.html#showcase)).

# Platforms / Architectures [![Latest build from main branch](https://github.com/kitsumed/ongakuvault/actions/workflows/build-test-publish-asp.net-core.yml/badge.svg?branch=main)](https://github.com/kitsumed/ongakuvault/actions/workflows/build-test-publish-asp.net-core.yml)

| Specificities                                                                                        | win x32 | win x64 | arm (x32)                           | arm64 | amd64 |
|------------------------------------------------------------------------------------------------------|---------|---------|-------------------------------------|-------|-------|
| [Available as binary](https://kitsumed.github.io/ongakuvault/installation.html#from-binary-releases) | ✅      | ✅      | ✅                                | ✅    | ✅    |
| [Available as docker image](https://kitsumed.github.io/ongakuvault/installation.html#from-docker)    | ❌      | ❌      | ✅                                | ✅    | ✅    |
| All third-party binaries included                                                                    | ✅      | ✅      | ⚠️ (only includes **yt-dlp**[^1]) | ✅    | ✅    |

> [!NOTE]
> All builds are made using Github actions.

[^1]: [See **IMPORTANT** note in documentation](https://kitsumed.github.io/ongakuvault/installation.html#installation-methods).

# TODO
- [ ] Add support for `lyrics` and `synced lyrics` metadata.

## License
```txt
Copyright 2025 kitsumed (Med)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
```
## Acknowledgements / Notice
Third-party acknowledgements & licenses can be found inside [the NOTICE file](OngakuVault/NOTICE).