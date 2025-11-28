FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://*:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PlantCareBot/PlantCareBot.csproj", "PlantCareBot/"]
RUN dotnet restore "PlantCareBot/PlantCareBot.csproj"
COPY . .
WORKDIR "/src/PlantCareBot"
RUN dotnet build "PlantCareBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PlantCareBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PlantCareBot.dll"]