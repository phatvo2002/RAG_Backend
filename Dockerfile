FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src
COPY . .

RUN echo "===== DOMAIN =====" && \
    dotnet restore "src/Nexora.Domain/Nexora.Domain.csproj" --verbosity normal

RUN echo "===== APPLICATION =====" && \
    dotnet restore "src/Nexora.Application/Nexora.Application.csproj" --verbosity normal

RUN echo "===== INFRASTRUCTURE =====" && \
    dotnet restore "src/Nexora.Infrastructure/Nexora.Infrastructure.csproj" --verbosity normal

RUN echo "===== WEBAPI =====" && \
    dotnet restore "src/Nexora.WebApi/Nexora.WebApi.csproj" --verbosity normal

RUN dotnet publish "src/Nexora.WebApi/Nexora.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Nexora.WebApi.dll"]