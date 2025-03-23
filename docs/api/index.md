# API Usage

OngakuVault  provide a web interface with API documentations using Swagger. Accessible at `/swagger` on the root of your OngakuVault website, It is disabled by default in production builds. You can enable it by referring to the available [configurations](../configurations/).

> [!NOTE]
> A WebSocket endpoint to receive live updates from the server is also available.
> The WebSocket endpoint and its responses where not included in the Swagger documentation as I was unable to add them using the NuGet package [`Swashbuckle.AspNetCore`](https://github.com/domaindrivendev/Swashbuckle.AspNetCore). They are instead available here under the [WebSocket Docs](./websocket-docs) page.