FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY api/src/Fluently.API/Fluently.API.csproj api/src/Fluently.API/
RUN dotnet restore api/src/Fluently.API/Fluently.API.csproj

COPY api/src/Fluently.API api/src/Fluently.API
RUN dotnet publish api/src/Fluently.API/Fluently.API.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Fluently.API.dll"]
