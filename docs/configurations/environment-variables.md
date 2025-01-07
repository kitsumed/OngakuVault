# Environment Variables
> [!NOTE]  
> Ongakuvault uses [ASP.NET Configuration providers](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0) to allow you to define application settings in multiple ways.
> This page lists all the available [environment variables](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0#non-prefixed-environment-variables), but you can also
> configure the application using [appsettings.json](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0#appsettingsjson) or by using
> [cmd/bash arguments](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0#command-line-arguments) when starting the application.
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
| `Ongaku__OUTPUT_DIRECTORY`          | Directory for downloaded audio files.                                     | *empty*, defaults to `./archived-audios`    | `/home/app/archived-audios`                 |
| `Ongaku__TMP_OUTPUT_DIRECTORY`      | Temporary directory for downloading audio, ⚠️**deleted at app closure**. | Temp directory created in OS Temp directory | Temp directory created in OS Temp directory |
| `Ongaku__ENABLE_SWAGGER_DOC`        | Enables Swagger API doc at `/swagger`.                                    | `false`                                     | `false`                                     |
| `Ongaku__DISABLE_WEBSITE`           | Disables the website at root `/`, leaving only the API.                   | `false`                                     | `false`                                     |
| `Ongaku__ENFORCE_HTTPS`             | If `true`, redirects HTTP to HTTPS.                                       | `false`                                     | `false`                                     |
| `Ongaku__PARALLEL_SCRAPPER_PROC`    | Set the number of parallel scrapper processes allowed. (**Max ``100``**)  | `8`                                         | `8`                                         |
| `Ongaku__PARALLEL_JOBS`             | Set the number of download jobs allowed to run at the same time.<br>(**Should be less than `PARALLEL_SCRAPPER_PROC` to leave rooms for information scrapping processes**.)| `4` | `4` |