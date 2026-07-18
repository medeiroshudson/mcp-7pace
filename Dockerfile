FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project for layer caching
COPY SevenPaceMcp.sln ./
COPY src/SevenPaceMcp/SevenPaceMcp.csproj src/SevenPaceMcp/

# Restore
RUN dotnet restore src/SevenPaceMcp/SevenPaceMcp.csproj

# Copy source
COPY . .

# Publish
RUN dotnet publish src/SevenPaceMcp/SevenPaceMcp.csproj -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Defaults; override at runtime
ENV SEVENPACE_ORGANIZATION="" \
    SEVENPACE_TOKEN="" \
    SEVENPACE_BASE_URL="" \
    SEVENPACE_WRITE_TIMEOUT_MS="30000" \
    PORT="" \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/out .

# MCP communicates over stdio by default; set PORT to enable HTTP transport
ENTRYPOINT ["dotnet", "SevenPaceMcp.dll"]