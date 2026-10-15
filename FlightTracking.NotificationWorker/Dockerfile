FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FlightTracking.NotificationWorker.csproj", "./"]
RUN dotnet restore "FlightTracking.NotificationWorker.csproj"
COPY . .
RUN dotnet build "FlightTracking.NotificationWorker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FlightTracking.NotificationWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FlightTracking.NotificationWorker.dll"]
