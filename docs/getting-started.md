# Getting started

This page will explain the use cases of Ongakuvault, what it is, and what it isn't.

If you want to install it, [go here](/installation).
## What is Ongakuvault?

Ongakuvault is an ASP.NET application running on .NET Core 8, which enables it to run seamlessly on both Windows and Linux. It allows you to download audio files, preserve their metadata, and modify selected fields for better control over your audio archive.

## Showcase
> [!NOTE]
> Current showcase is from version ``1.0.0``, your version may differ.

### Desktop
![Desktop showcase](/showcase_website_desktop.png)
::: details See video
<video autoplay loop controls>
  <source src="/showcase_website_desktop_bigduckbunny.mp4" type="video/mp4">
</video> 
:::

### Mobile
![Mobile showcase](/showcase_website_mobile.png)

## How do Ongakuvault work?

Ongakuvault is an API that operates based on '**Jobs**'. Each job represents a website to be scraped for media to be downloaded. You can choose from popular file formats like **MP3**, **FLAC**, or **WAV**, while retaining the original audio metadata. When creating a job, you also have the option to overwrite specific metadata fields. Additionally, the Ongakuvault API can be used to fetch media information from a webpage without downloading it. Ongakuvault itself does not scrape websites, it relies on yt-dlp, FFmpeg, and FFprobe for these tasks.

## Will Ongakuvault allow me to browse my archived audio files?

No, Ongakuvault's primary goal is to provide a remote interface and API for downloading files to a server and moving them in the final output folder (see [env variable configurations](./configurations/environment-variables)). However, you can combine Ongakuvault with a streaming application like [Jellyfin](https://github.com/jellyfin/jellyfin) or [Navidrome](https://github.com/navidrome/navidrome/) to browse and stream your archived audio files.

## What website are supported?

Ongakuvault itself does not directly support specific websites. Instead, it relies on [yt-dlp](https://github.com/yt-dlp/yt-dlp), which supports a wide range of websites. You can view the full list of supported sites (also known as "extractors") [here](https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md). Additionally, yt-dlp includes a "general" extractor for websites not listed. You can also provide a direct URL to your media file, and yt-dlp will handle the rest.

**If you encounter issues related to the scrapper** like unsupported sites or failed scraping attempts for unknown reasons, **please report them directly to the [yt-dlp repository](https://github.com/yt-dlp/yt-dlp/issues)**. **Ongakuvault won't be able to fix theses, so issues related to the scrapper should be addressed there**.

## Sidenote / story
I started Ongakuvault as a project because I was tired of manually editing my audio file metadata, dealing with SFTP connections, and transferring everything to my server. Then, I had an idea: 
> *What if I could make an app that would let me download a audio from a direct URL and edit the metadata at the same time*?

And that's how Ongakuvault was born. From one person idea, Japanese music playing in the background, with "Ongaku" meaning music in Japanese, and "Vault" referring to an archive/collection.