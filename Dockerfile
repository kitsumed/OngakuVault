# .NET 8.0 ASP.NET CHISELED image. https://github.com/dotnet/dotnet-docker/blob/main/documentation/image-variants.md & https://github.com/dotnet/dotnet-docker/tree/main/src/aspnet/8.0
# This Dockerfile was made to be used by Github Actions.
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled
WORKDIR /app

# Define a build argument for the target platform-specific architecture
ARG TARGETARCH

# Set the default environment variables for ASP.NET Core
ENV ASPNETCORE_HTTP_PORTS=8080
#ENV ASPNETCORE_HTTPS_PORT=8443
ENV ASPNETCORE_ENVIRONMENT=Production
# Other env variable name can be found on https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/web-host?view=aspnetcore-8.0#host-configuration-values

# Default port to expose
EXPOSE ${ASPNETCORE_PORT}

# Copy the build binary for the appropriate platform. Hard-coded for linux based binary.
COPY ./build/linux-${TARGETARCH}-build/. ./ 

# Enable non-root user as default user
USER $APP_UID
# Execute OngakuVault
ENTRYPOINT ["dotnet", "OngakuVault.dll"]