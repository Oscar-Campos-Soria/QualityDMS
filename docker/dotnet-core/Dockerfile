# ── .NET 10 — Multi-stage build ──
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaurar dependencias primero (cache layer)
COPY ["CalidadSYS/CalidadSYS.csproj",                                       "CalidadSYS/"]
COPY ["src/QualityDMS.Domain/QualityDMS.Domain.csproj",                     "src/QualityDMS.Domain/"]
COPY ["src/QualityDMS.Application/QualityDMS.Application.csproj",           "src/QualityDMS.Application/"]
COPY ["src/QualityDMS.Infrastructure/QualityDMS.Infrastructure.csproj",     "src/QualityDMS.Infrastructure/"]
RUN dotnet restore "CalidadSYS/CalidadSYS.csproj"

COPY . .
RUN dotnet publish "CalidadSYS/CalidadSYS.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CalidadSYS.dll"]
