# syntax=docker/dockerfile:1

# ---- Etapa 1: compilación y publicación ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar solo los .csproj para cachear la restauración de paquetes
COPY src/VetClinic.Domain/VetClinic.Domain.csproj src/VetClinic.Domain/
COPY src/VetClinic.Application/VetClinic.Application.csproj src/VetClinic.Application/
COPY src/VetClinic.Infrastructure/VetClinic.Infrastructure.csproj src/VetClinic.Infrastructure/
COPY src/VetClinic.Api/VetClinic.Api.csproj src/VetClinic.Api/
RUN dotnet restore src/VetClinic.Api/VetClinic.Api.csproj

# Copiar el resto del código y publicar en Release
COPY . .
RUN dotnet publish src/VetClinic.Api/VetClinic.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- Etapa 2: imagen de ejecución ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Development \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "VetClinic.Api.dll"]
