# Multi-stage Dockerfile for OpenRFID Middleware
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project files first for optimal layer caching
COPY ["OpenRFID.slnx", "./"]
COPY ["src/OpenRFID.Core.Abstractions/OpenRFID.Core.Abstractions.csproj", "src/OpenRFID.Core.Abstractions/"]
COPY ["src/OpenRFID.Core.Dispatch/OpenRFID.Core.Dispatch.csproj", "src/OpenRFID.Core.Dispatch/"]
COPY ["src/OpenRFID.Core.Pipeline/OpenRFID.Core.Pipeline.csproj", "src/OpenRFID.Core.Pipeline/"]
COPY ["src/OpenRFID.Core.Storage/OpenRFID.Core.Storage.csproj", "src/OpenRFID.Core.Storage/"]
COPY ["src/OpenRFID.Core.Engine/OpenRFID.Core.Engine.csproj", "src/OpenRFID.Core.Engine/"]
COPY ["src/OpenRFID.Management.Api/OpenRFID.Management.Api.csproj", "src/OpenRFID.Management.Api/"]
COPY ["src/OpenRFID.Simulator/OpenRFID.Simulator.csproj", "src/OpenRFID.Simulator/"]

RUN dotnet restore "src/OpenRFID.Management.Api/OpenRFID.Management.Api.csproj"

# Copy full source tree and publish
COPY . .
WORKDIR "/src/src/OpenRFID.Management.Api"
RUN dotnet publish "OpenRFID.Management.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OpenRFID.Management.Api.dll"]
