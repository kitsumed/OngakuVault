# Environment Variables
> [!NOTE]  
> Ongakuvault uses [ASP.NET Configuration providers](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0) to allow you to define application settings in multiple ways.
> This page lists all the available [environment variables](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0#non-prefixed-environment-variables), but you can also
> configure the application using [appsettings.json](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0#appsettingsjson) or by using
> [cmd/bash arguments](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0#command-line-arguments) when starting the application.

> [!TIP]  
> Some variables have an additional detailed description. You can access them by clicking on the variable name.

## Basic configuration
| Environment Variable                                | Description                            | Default Value                    | Docker Default Value |
|-----------------------------------------------------|----------------------------------------|----------------------------------|----------------------|
| `ASPNETCORE_HTTP_PORTS`                             | Port used for HTTP connections.        | `8080`                           | `8080`               |
| `ASPNETCORE_HTTPS_PORT`                             | Port used for HTTPS connections.       | `8443`                           | `8443`               |
| `ASPNETCORE_Kestrel__Certificates__Default__Path`   | Path to the SSL certificate for https. | *empty*                          | *empty*              |
| `ASPNETCORE_Kestrel__Certificates__Default__Password`| Password for the SSL certificate.     | *empty*                          | *empty*              |

## Advanced configuration
| Environment Variable        | Description                                                               | Default Value                               | Docker Default Value                        |
|-----------------------------|---------------------------------------------------------------------------|---------------------------------------------|---------------------------------------------|
| [`Ongaku__CLEAR_METADATA_NONSTANDARD_FIELDS`](#clear-metadata-nonstandard-fields)| Removes additionals (non-standard) fields from your files metadata.| `false`                                     | `false`                                     |
| `Ongaku__DISABLE_WEBSITE`           | Disables the website at root `/`, leaving only the API.                   | `false`                                     | `false`                                     |
| `Ongaku__ENABLE_SWAGGER_DOC`        | Enables Swagger API doc at `/swagger`.                                    | `false`                                     | `false`                                     |
| `Ongaku__ENFORCE_HTTPS`             | If `true`, redirects HTTP to HTTPS.                                       | `false`                                     | `false`                                     |
| [`Ongaku__LYRICS_LANGUAGE_PRIORITY`](#lyrics-language-priority)  | Lyrics/subtitle languages to prioritize when available. (IETF tags)    | *Empty*, lyrics fetching disabled.          | *empty*           |
| `Ongaku__OUTPUT_DIRECTORY`          | Directory for downloaded audio files.                                     | *empty*, defaults to `./archived-audios`    | `/home/app/archived-audios`                 |
| [`Ongaku__OUTPUT_SUB_DIRECTORY_FORMAT`](#output-sub-directory-format)| Sub directory for downloaded audio files.| *empty*                                     |*empty*                                      |
| `Ongaku__PARALLEL_SCRAPER_PROC`    | Set the number of parallel scraper processes allowed. (**Max ``100``**)  | `8`                                         | `8`                                         |
| `Ongaku__PARALLEL_JOBS`             | Set the number of download jobs allowed to run at the same time.<br>(**Should be less than `PARALLEL_SCRAPER_PROC` to leave rooms for information scrapping processes**.)| `4` | `4` |
| `Ongaku__TMP_OUTPUT_DIRECTORY`      | Temporary directory for downloading audio, ⚠️**deleted at app closure**. | Temp directory created in OS Temp directory | Temp directory created in OS Temp directory |

## Detailed descriptions
### CLEAR_METADATA_NONSTANDARD_FIELDS
The `CLEAR_METADATA_NONSTANDARD_FIELDS` variable allows you to remove every fields that are not part of the metadata specs (like ID3v2) standard format. They are also known as *additionals* fields.
This project uses [atldotnet](https://github.com/Zeugma440/atldotnet) to edit the files metadata, to learn more about what is considered a non-standard field, read the [ATL wiki about non standard fields](https://github.com/Zeugma440/atldotnet/wiki/Focus-on-non-standard-fields#what-are-non-standard-fields-).

### LYRICS_LANGUAGE_PRIORITY
The `LYRICS_LANGUAGE_PRIORITY` variable uses [IETF tags](https://wikipedia.org/wiki/IETF_language_tag) returned by the scraper yt-dlp to select, if found, the lyrics language.
You can define multiples languages by separating them of a PIPE (`|`) character.

For example, `en-US|en|fr` would prioritize **en-US** language, fallback to **en**, and finally fallback to **fr**.
If left empty, the application will not fetch any lyrics/subtitle, even if yt-dlp found any.

You can find a list of all existing [IETF tags here](https://www.iana.org/assignments/language-subtag-registry/language-subtag-registry).

### OUTPUT_SUB_DIRECTORY_FORMAT
The `OUTPUT_SUB_DIRECTORY_FORMAT` variable allow for downloaded files to be organised by sub directory insead of beeing directly added to the base output directory.
You can set dynamic values by surrounding them of a PIPE (`|`) character.

**Dynamic values**: `NOW_YEAR`,`NOW_MONTH`,`NOW_DAY`,`AUDIO_ARTIST`,`AUDIO_ALBUM`,`AUDIO_YEAR`

**Example**:`|AUDIO_ARTIST|\|AUDIO_ALBUM|` would result to **BASE_OUTPUT_DIRECTORY** > **Artist** > **Album**.