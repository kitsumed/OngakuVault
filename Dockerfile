# .NET 8.0 ASP.NET CHISELED image. No shell, https://github.com/dotnet/dotnet-docker/blob/main/documentation/image-variants.md & https://github.com/dotnet/dotnet-docker/tree/main/src/aspnet/8.0
# This Dockerfile was made to be used by Github Actions.
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled

WORKDIR /app

# Define a build argument for the target platform-specific architecture
ARG TARGETARCH

# Set the default environment variables for ASP.NET Core : Other names can be found on https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/web-host?view=aspnetcore-8.0#host-configuration-values
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_HTTPS_PORT=8443
ENV ENFORCE_HTTPS=false
ENV ASPNETCORE_ENVIRONMENT=Production
# Overwrite Kestrel settings to set the https certificate config if HTTPS is enforced. Source : https://medium.com/@davidclaeys/deploying-net-containers-in-docker-463fc9e0a84e
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/home/ongakuvault/certificate.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=Password-Here
# App environment
ENV OUTPUT_DIRECTORY=/home/ongakuvault/archived-audios/

# Default port to expose
EXPOSE ${ASPNETCORE_HTTP_PORTS}
EXPOSE ${ASPNETCORE_HTTPS_PORT}

# Copy the build binary for the appropriate platform. Hard-coded for linux based binary.
COPY ./build/linux-${TARGETARCH}-build/. ./

# Enable non-root user as default user
USER $APP_UID
# Execute OngakuVault
ENTRYPOINT ["dotnet", "OngakuVault.dll"]