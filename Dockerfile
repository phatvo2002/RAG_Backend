FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Tắt telemetry và tối ưu MSBuild trong container để tránh treo tiến trình
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=true \
    MSBUILDDISABLENODEREUSE=1 \
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false

# Copy project files
COPY ["src/Nexora.Domain/Nexora.Domain.csproj", "src/Nexora.Domain/"]
COPY ["src/Nexora.Application/Nexora.Application.csproj", "src/Nexora.Application/"]
COPY ["src/Nexora.Infrastructure/Nexora.Infrastructure.csproj", "src/Nexora.Infrastructure/"]
COPY ["src/Nexora.WebApi/Nexora.WebApi.csproj", "src/Nexora.WebApi/"]

# Thêm --disable-parallel và -m:1 để ổn định kết nối tải package
RUN dotnet restore "./src/Nexora.WebApi/Nexora.WebApi.csproj" --disable-parallel -m:1

COPY . .
WORKDIR "/src/src/Nexora.WebApi"
RUN dotnet build "./Nexora.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore -m:1

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Nexora.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-build

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Nexora.WebApi.dll"]