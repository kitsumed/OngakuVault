# .NET 8.0 ASP.NET image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Define the path of build binary for a platform-specific build
ARG BUILD_PATH

# Set the default environment variables for ASP.NET Core
ENV ASPNETCORE_PORT=8080
# Listen on all interfaces
ENV ASPNETCORE_URLS=http://+:${ASPNETCORE_PORT}
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE ${ASPNETCORE_PORT}

# Copy the build artifacts for the appropriate platform
COPY ${BUILD_PATH}/. ./ 

# Execute OngakuVault
ENTRYPOINT ["dotnet", "OngakuVault.dll"]
