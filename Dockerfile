# Базовый образ для .NET 8.0
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://*:8080

# Сборка приложения
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем ВСЕ файлы проекта
COPY . .

# Восстанавливаем зависимости и собираем
RUN dotnet restore "PlantCareBot/PlantCareBot.csproj"
RUN dotnet build "PlantCareBot/PlantCareBot.csproj" -c Release -o /app/build

# Публикуем приложение
FROM build AS publish
RUN dotnet publish "PlantCareBot/PlantCareBot.csproj" -c Release -o /app/publish

# Финальный образ
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PlantCareBot.dll"]