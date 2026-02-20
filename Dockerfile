# Use official .NET 8 SDK image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY ["iBITS Portal/iBITS Portal.csproj", "iBITS Portal/"]
WORKDIR "/app/iBITS Portal"
RUN dotnet restore

# Copy everything else and build
WORKDIR /app
COPY ["iBITS Portal/", "iBITS Portal/"]
WORKDIR "/app/iBITS Portal"
RUN dotnet publish -c Release -o /app/out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Expose port
EXPOSE 8080

# Set environment variable for port
ENV ASPNETCORE_URLS=http://+:8080

# Start the application - FIXED: Use the DLL name directly
ENTRYPOINT ["dotnet", "iBITS Portal.dll"]
