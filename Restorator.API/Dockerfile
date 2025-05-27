# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled-extra AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:8.0-noble AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Restorator.API/Restorator.API.csproj", "Restorator.API/"]
COPY ["Restorator.Application/Restorator.Application.csproj", "Restorator.Application/"]
COPY ["Restorator.DataAccess/Restorator.DataAccess.csproj", "Restorator.DataAccess/"]
COPY ["Restorator.Domain/Restorator.Domain.csproj", "Restorator.Domain/"]
COPY ["Restorator.Mail/Restorator.Mail.csproj", "Restorator.Mail/"]
COPY ["Restorator.Seeder/Restorator.Seeder.csproj", "Restorator.Seeder/"]
RUN dotnet restore "./Restorator.API/Restorator.API.csproj"
COPY . .
WORKDIR "/src/Restorator.API"
RUN dotnet build "./Restorator.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Restorator.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Restorator.API.dll"]