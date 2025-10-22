# base
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Services.CatalogService/Services.CatalogService.csproj", "Services.CatalogService/"]
RUN dotnet restore "Services.CatalogService/Services.CatalogService.csproj"
COPY . .
WORKDIR "/src/Services.CatalogService"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "Services.CatalogService.dll"]
