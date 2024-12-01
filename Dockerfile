# .NET 8.0 ASP.NET CHISELED image. No shell, rootless by default, https://github.com/dotnet/dotnet-docker/blob/main/documentation/image-variants.md
# This Dockerfile was made to be used by Github Actions.
FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled
WORKDIR /app

# Define a build argument for the target platform-specific architecture
ARG TARGETARCH

# Set the default environment variables for ASP.NET Core
ENV ASPNETCORE_PORT=5000
# Listen on all interfaces
ENV ASPNETCORE_URLS=http://+:${ASPNETCORE_PORT}
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE ${ASPNETCORE_PORT}

# Copy the build binary for the appropriate platform. Hard-coded for linux based binary.
COPY ./build/linux-${TARGETARCH}-build/. ./ 

# Execute OngakuVault
ENTRYPOINT ["dotnet", "OngakuVault.dll"]