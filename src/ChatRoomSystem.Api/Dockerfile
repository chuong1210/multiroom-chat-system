# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files và restore dependencies
COPY ["src/ChatRoomSystem.Api/ChatRoomSystem.Api.csproj", "ChatRoomSystem.Api/"]
COPY ["src/ChatRoomSystem.Data/ChatRoomSystem.Data.csproj", "ChatRoomSystem.Data/"]
COPY ["src/ChatRoomSystem.Shared/ChatRoomSystem.Shared.csproj", "ChatRoomSystem.Shared/"]

RUN dotnet restore "ChatRoomSystem.Api/ChatRoomSystem.Api.csproj"

# Copy toàn bộ source code
COPY src/ .

# Build project
WORKDIR "/src/ChatRoomSystem.Api"
RUN dotnet build "ChatRoomSystem.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ChatRoomSystem.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl cho healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=publish /app/publish .

# Create data directory cho SQLite
RUN mkdir -p /app/data

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run application
ENTRYPOINT ["dotnet", "ChatRoomSystem.Api.dll"]
