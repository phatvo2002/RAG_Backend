FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

WORKDIR /app

EXPOSE 8080
EXPOSE 8081


FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=true \
    NUGET_XMLDOC_MODE=skip

COPY . .

RUN dotnet --info

RUN echo "===== NUGET SOURCES =====" && \
    dotnet nuget list source

RUN echo "===== CHECK NUGET CONNECTION =====" && \
    wget -S --spider https://api.nuget.org/v3/index.json

RUN echo "===== RESTORE WEBAPI =====" && \
    dotnet restore "src/Nexora.WebApi/Nexora.WebApi.csproj" \
    --source https://api.nuget.org/v3/index.json \
    -p:NuGetAudit=false \
    --verbosity normal

RUN echo "===== PUBLISH WEBAPI =====" && \
    dotnet publish "src/Nexora.WebApi/Nexora.WebApi.csproj" \
    --configuration $BUILD_CONFIGURATION \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false


FROM base AS final

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Nexora.WebApi.dll"]