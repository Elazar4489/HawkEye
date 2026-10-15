FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FlightProducer.csproj", "./"]
RUN dotnet restore "FlightProducer.csproj"
COPY . .
RUN dotnet build "FlightProducer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FlightProducer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FlightProducer.dll"]