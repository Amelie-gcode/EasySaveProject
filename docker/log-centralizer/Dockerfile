FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY LogCentralizer.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV LOGS_DIRECTORY=/data/logs
EXPOSE 8080
COPY --from=build /app/publish .
RUN mkdir -p /data/logs
VOLUME ["/data/logs"]
ENTRYPOINT ["dotnet", "EasySave.LogCentralizer.dll"]
