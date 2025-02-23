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
| `Ongaku__DISABLE_WEBSITE`           | Disables the website at root `/`, leaving only the API.                   | `false`                                     | `false`                             |
| `Ongaku__ENABLE_SWAGGER_DOC`        | Enables Swagger API doc at `/swagger`.                                    | `false`                                     | `false`                             |
| `Ongaku__ENFORCE_HTTPS`             | If `true`, redirects HTTP to HTTPS.                                       | `false`                                     | `false`                             |
| [`Ongaku__LYRICS_LANGUAGE_PRIORITY`](#lyrics-language-priority)  | Lyrics/subtitle languages to prioritize when available. (IETF tags)    | *Empty*, lyrics fetching disabled.          | *empty*   |
| `Ongaku__OUTPUT_DIRECTORY`          | Output directory for downloaded audio files.                              | *empty*, defaults to `./archived-audios`    | `/home/app/archived-audios`         |
| [`Ongaku__OUTPUT_FILE_FORMAT`](#output-file-format)| Downloaded files naming template                           | *empty*                                     | *empty*                             |
| [`Ongaku__OUTPUT_SUB_DIRECTORY_FORMAT`](#output-sub-directory-format)| Sub directory for downloaded audio files.| *empty*                                     | *empty*                             |
| `Ongaku__PARALLEL_SCRAPER_PROC`     | Set the number of parallel scraper processes allowed. (**Max ``100``**)   | `8`                                         | `8`                                 |
| `Ongaku__PARALLEL_JOBS`             | Set the number of download jobs allowed to run at the same time.<br>(**Should be less than `PARALLEL_SCRAPER_PROC` to leave rooms for information scrapping processes**.)| `4` | `4` |
| `Ongaku__TMP_OUTPUT_DIRECTORY`      | Temporary directory for downloading audio, ⚠️**deleted at app closure**. | Temp directory created in OS Temp directory | Temp directory created in OS Temp directory |



## Detailed descriptions
If you are unsure about the usage of a variable and do not find a detailed description here, take a look at the [descriptions in the source code](https://github.com/kitsumed/OngakuVault/blob/main/OngakuVault/Models/AppSettingsModel.cs).

### CLEAR_METADATA_NONSTANDARD_FIELDS
The `CLEAR_METADATA_NONSTANDARD_FIELDS` variable allows you to remove every fields that are not part of the metadata specs (like ID3v2) standard format. They are also known as *additionals* fields.
This project uses [atldotnet](https://github.com/Zeugma440/atldotnet) to edit the files metadata, to learn more about what is considered a non-standard field, read the [ATL wiki about non standard fields](https://github.com/Zeugma440/atldotnet/wiki/Focus-on-non-standard-fields#what-are-non-standard-fields-).

### LYRICS_LANGUAGE_PRIORITY
The `LYRICS_LANGUAGE_PRIORITY` variable uses [IETF tags](https://wikipedia.org/wiki/IETF_language_tag) returned by the scraper yt-dlp to select, if found, the lyrics language.
You can define multiples languages by separating them of a PIPE (`|`) character.

For example, `en-US|en|fr` would prioritize **en-US** language, fallback to **en**, and finally fallback to **fr**.
If left empty, the application will not fetch any lyrics/subtitle, even if yt-dlp found any.

You can find a list of all existing [IETF tags here](https://www.iana.org/assignments/language-subtag-registry/language-subtag-registry).

### OUTPUT_FILE_FORMAT
The `OUTPUT_FILE_FORMAT` variable allow for the definition of a template that will be used to rename the downloaded file name.
To set dynamic values, refer yourself to the values supported by the used replacing processors.

**Values processors used with this parameter**: [DATE](#date-processor), [TRACK](#track-processor)

**Example**: `|AUDIO_TITLE|-|AUDIO_YEAR|` would result to **``My_Title-2020``**.
>[!WARNING]
> The file format only concerns the file name, if you include a file extension it will only results in a file with "two" file extension.
> **Example:** `processorsResults.mp3.mp3`

### OUTPUT_SUB_DIRECTORY_FORMAT
The `OUTPUT_SUB_DIRECTORY_FORMAT` variable allow for downloaded files to be organised by sub directory insead of beeing directly added to the base output directory.
To set dynamic values, refer yourself to the values supported by the used replacing processors.

**Values processors used with this parameter**: [DATE](#date-processor), [TRACK](#track-processor)

**Example**: `|AUDIO_ARTIST|\|AUDIO_ALBUM|` would result to **BASE_OUTPUT_DIRECTORY** > **Artist** > **Album**.



## Value Replacing (Dynamic)
Value replacing is the name given to strings surrounded by two PIPE (`|`) character that get replaced with a specific value during runtime.
You can see the helper class that manage value replacing in the source code [here](#soon).

For example, the [Date Processor](#date-processor) would replace `|NOW_YEAR|` with the current year.
### Date Processor
| Dynamic Value        | Description                                                               |
|----------------------|---------------------------------------------------------------------------|
| `NOW_YEAR`           | Current year                                                              |
| `NOW_MONTH`          | Current month                                                             |
| `NOW_DAY`            | Current day                                                               |
| `NOW_SECOND`         | Current seconds                                                           |
| `NOW_TICKS`          | Current time in ticks (timestamp)                                         |

### Track Processor
>[!NOTE]
>Track is a class from the ATL library containing a file metadata.

| Dynamic Value        | Description                                                               |
|----------------------|---------------------------------------------------------------------------|
| `AUDIO_TITLE`        | Current track title **OR** ``Unknown``                                    |
| `AUDIO_ARTIST`       | Current track artist **OR** ``Unknown``                                   |
| `AUDIO_ALBUM`        | Current track album **OR** ``Unknown``                                    |
| `AUDIO_YEAR`         | Current track year **OR** ``0``                                           |
| `AUDIO_TRACK_NUMBER` | Current track number **OR** ``0``                                         |
| `AUDIO_LANGUAGE`     | Current track language **OR** ``Unknown``                                 |
| `AUDIO_GENRE`        | Current track genre **OR** ``Unknown``                                    |
| `AUDIO_COMPOSER`     | Current track composer **OR** ``Unknown``                                 |