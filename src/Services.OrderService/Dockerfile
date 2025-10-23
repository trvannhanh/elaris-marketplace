# ===============================
# STAGE 1: BUILD
# ===============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj trước để restore
COPY ["Services.OrderService/Services.OrderService.csproj", "Services.OrderService/"]
COPY ["BuildingBlocks/Contracts/BuildingBlocks.Contracts.csproj", "BuildingBlocks/Contracts/"]

RUN dotnet restore "Services.OrderService/Services.OrderService.csproj"

# copy toàn bộ src (cả BuildingBlocks)
COPY . .

WORKDIR "/src/Services.OrderService"
RUN dotnet publish -c Release -o /app/publish

# ===============================
# STAGE 2: RUNTIME
# ===============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "Services.OrderService.dll"]
