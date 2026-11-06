# syntax=docker/dockerfile:1
# Build from the repo root: docker build -f src/ModApi.Api/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/ModApi.Domain/ModApi.Domain.csproj src/ModApi.Domain/
COPY src/ModApi.Application/ModApi.Application.csproj src/ModApi.Application/
COPY src/ModApi.Infrastructure/ModApi.Infrastructure.csproj src/ModApi.Infrastructure/
COPY src/ModApi.Api/ModApi.Api.csproj src/ModApi.Api/
RUN dotnet restore src/ModApi.Api/ModApi.Api.csproj

COPY src/ src/
RUN dotnet publish src/ModApi.Api/ModApi.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# TLS is handled by the ingress, the app itself listens on plain http
ENV ASPNETCORE_HTTP_PORTS=8080 \
    Hosting__UseHttpsRedirection=false

EXPOSE 8080
COPY --from=build /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "ModApi.Api.dll"]
