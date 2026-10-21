# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first for better layer caching
COPY ["OrbitCRM.Web/OrbitCRM.Web.csproj", "OrbitCRM.Web/"]
COPY ["OrbitCRM.Application/OrbitCRM.Application.csproj", "OrbitCRM.Application/"]
COPY ["OrbitCRM.Domain/OrbitCRM.Domain.csproj", "OrbitCRM.Domain/"]
COPY ["OrbitCRM.Infrastructure/OrbitCRM.Infrastructure.csproj", "OrbitCRM.Infrastructure/"]

# Restore
RUN dotnet restore "OrbitCRM.Web/OrbitCRM.Web.csproj"

# Copy everything else
COPY . .

# Build
WORKDIR "/src/OrbitCRM.Web"
RUN dotnet publish "OrbitCRM.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "OrbitCRM.Web.dll"]
