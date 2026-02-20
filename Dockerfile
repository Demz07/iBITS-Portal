# Use official .NET 8 SDK image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY "iBITS Portal/*.csproj" "./iBITS Portal/"
WORKDIR "/app/iBITS Portal"
RUN dotnet restore

# Copy everything else and build
WORKDIR /app
COPY "iBITS Portal/" "./iBITS Portal/"
WORKDIR "/app/iBITS Portal"
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build "/app/iBITS Portal/out" .

# Expose port
EXPOSE 8080

# Set environment variable for port
ENV ASPNETCORE_URLS=http://+:8080

# Start the application
ENTRYPOINT ["dotnet", "iBITS Portal.dll"]
