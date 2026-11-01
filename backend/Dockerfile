# ---------- Etapa 1: compilación ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar primero solo los .csproj aprovecha la caché de Docker: el restore solo se repite
# cuando cambian las dependencias, no en cada cambio de código.
COPY src/Planillas.Domain/Planillas.Domain.csproj src/Planillas.Domain/
COPY src/Planillas.Application/Planillas.Application.csproj src/Planillas.Application/
COPY src/Planillas.Infrastructure/Planillas.Infrastructure.csproj src/Planillas.Infrastructure/
COPY src/Planillas.Api/Planillas.Api.csproj src/Planillas.Api/
RUN dotnet restore src/Planillas.Api/Planillas.Api.csproj

COPY src/ src/
RUN dotnet publish src/Planillas.Api/Planillas.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Etapa 2: runtime (sin SDK, imagen más pequeña) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    TZ=America/Lima
COPY --from=build /app/publish .

# Usuario sin privilegios incluido en las imágenes oficiales de .NET (UID 1654).
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Planillas.Api.dll"]
