# See https://aka.ms/customizecontainer to learn how to customize your debug container
# and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends tesseract-ocr tesseract-ocr-vie tesseract-ocr-eng \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/Nexora.Domain/Nexora.Domain.csproj", "src/Nexora.Domain/"]
COPY ["src/Nexora.Application/Nexora.Application.csproj", "src/Nexora.Application/"]
COPY ["src/Nexora.Infrastructure/Nexora.Infrastructure.csproj", "src/Nexora.Infrastructure/"]
COPY ["src/Nexora.WebApi/Nexora.WebApi.csproj", "src/Nexora.WebApi/"]
RUN dotnet restore "./src/Nexora.WebApi/Nexora.WebApi.csproj"

COPY . .
WORKDIR "/src/src/Nexora.WebApi"
RUN dotnet build "./Nexora.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Nexora.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM build AS migrate
WORKDIR /src
RUN dotnet tool restore
ENTRYPOINT ["dotnet", "ef", "database", "update", "--project", "src/Nexora.Infrastructure", "--startup-project", "src/Nexora.WebApi", "--context", "NexoraDbContext"]

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Nexora.WebApi.dll"]
