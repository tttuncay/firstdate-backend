# Multi-stage Dockerfile for ASP.NET Core 10 Web API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore
COPY ["DateSpot.Core/DateSpot.Core.csproj", "DateSpot.Core/"]
COPY ["DateSpot.Application/DateSpot.Application.csproj", "DateSpot.Application/"]
COPY ["DateSpot.Infrastructure/DateSpot.Infrastructure.csproj", "DateSpot.Infrastructure/"]
COPY ["DateSpot.Api/DateSpot.Api.csproj", "DateSpot.Api/"]

RUN dotnet restore "DateSpot.Api/DateSpot.Api.csproj"

# Copy remaining source code and build
COPY . .
WORKDIR "/src/DateSpot.Api"
RUN dotnet publish "DateSpot.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DateSpot.Api.dll"]
