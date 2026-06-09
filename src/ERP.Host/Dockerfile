FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/ERP.Host/ERP.Host.csproj", "src/ERP.Host/"]
COPY ["src/ERP.Shared/ERP.Shared.csproj", "src/ERP.Shared/"]
COPY ["src/Modules/ERP.Tenants/ERP.Tenants.csproj", "src/Modules/ERP.Tenants/"]
COPY ["src/Modules/ERP.Auth/ERP.Auth.csproj", "src/Modules/ERP.Auth/"]
COPY ["src/Modules/ERP.RBAC/ERP.RBAC.csproj", "src/Modules/ERP.RBAC/"]
COPY ["src/Modules/ERP.Users/ERP.Users.csproj", "src/Modules/ERP.Users/"]
RUN dotnet restore "src/ERP.Host/ERP.Host.csproj"

COPY . .
WORKDIR "/src/src/ERP.Host"
RUN dotnet build "ERP.Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ERP.Host.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ERP.Host.dll"]
