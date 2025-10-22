# base
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Services.OrderService/Services.OrderService.csproj", "Services.OrderService/"]
RUN dotnet restore "Services.OrderService/Services.OrderService.csproj"
COPY . .
WORKDIR "/src/Services.OrderService"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "Services.OrderService.dll"]
