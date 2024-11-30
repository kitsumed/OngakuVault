# .NET 8.0 ASP.NET image. This is a linux-based image that cannot build windows image.
# This Dockerfile was made to be used by Github Actions.
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Define the path of build binary directory for a platform-specific build
ARG BUILD_PATH
# Define a build argument for the target architecture
ARG TARGETARCH

# Set the default environment variables for ASP.NET Core
ENV ASPNETCORE_PORT=5000
# Listen on all interfaces
ENV ASPNETCORE_URLS=http://+:${ASPNETCORE_PORT}
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE ${ASPNETCORE_PORT}

# Copy the build binary for the appropriate platform. Hard-coded for linux based binary.
COPY ${BUILD_PATH}/linux-${TARGETARCH}-build/. ./ 

# Execute OngakuVault
ENTRYPOINT ["dotnet", "OngakuVault.dll"]
