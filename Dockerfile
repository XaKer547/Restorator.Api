FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled AS base
USER app
WORKDIR /app
EXPOSE 7090
EXPOSE 5029

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY . .

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Restorator.API/Restorator.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish -p:PublishTrimmed=false --runtime linux-x64 --self-contained true

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["/app/Restorator.API"]