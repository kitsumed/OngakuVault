# What is OngakuVault?
OngakuVault, derived from the word music in Japanese (音楽), is a C# music downloader designed to archive songs locally on a device.
# Installation
> [!IMPORTANT]  
> Builds for `linux/arm` do not include yt-dlp FFmpeg binaries. You will need to compile them yourself or [download the official one](https://ffmpeg.org/download.html) and place the binaries in your application root directory.
­­> For Docker users, ensure that both `ffmpeg` and `ffprobe` are mapped to `/app/ffmpeg` and `/app/ffprobe`.
> I do not want to automate the download of this version because the FFmpeg license requires us to include a link to or a way to access the open-source version of the code. This project uses a [modified version of FFmpeg from yt-dlp](https://github.com/yt-dlp/FFmpeg-Builds) for all other builds. To avoid confusion and potential issues, only the link to the yt-dlp version of FFmpeg is included in the notice file, meaning that only their binaries can be included into the releases.