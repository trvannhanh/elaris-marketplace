# ===== BUILD STAGE =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj
COPY *.sln ./
COPY ApiGateway/*.csproj ApiGateway/
RUN dotnet restore "ApiGateway/ApiGateway.csproj"

# copy toàn bộ code
COPY . .
RUN dotnet publish "ApiGateway/ApiGateway.csproj" -c Release -o /app/publish

# ===== RUNTIME STAGE =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
