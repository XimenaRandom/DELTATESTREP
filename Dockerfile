FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /src
COPY . .
RUN dotnet publish DELTAAPI/DELTAAPI.csproj -c Release -o /out/api --self-contained false
RUN dotnet publish DELTATEST/DELTATEST.csproj -c Release -o /out/client -p:BlazorEnableCompression=false
RUN mkdir -p /out/api/wwwroot && cp -a /out/client/wwwroot/. /out/api/wwwroot/ && printf '{"ApiBaseUrl":"/"}' > /out/api/wwwroot/appsettings.json

FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim
# Fonts used by PDF reports on Linux.
RUN apt-get update && apt-get install -y --no-install-recommends fontconfig fonts-dejavu-core && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /out/api/ .
ENV ASPNETCORE_ENVIRONMENT=Production
USER app
CMD ["sh", "-c", "exec dotnet DELTAAPI.dll --urls http://0.0.0.0:${PORT:-8080}"]
