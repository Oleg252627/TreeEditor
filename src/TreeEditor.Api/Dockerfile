# Build context: repository root (see compose.yaml).

FROM node:22-alpine AS client-build
WORKDIR /src/src/TreeEditor.Api/ClientApp

COPY src/TreeEditor.Api/ClientApp/package.json src/TreeEditor.Api/ClientApp/package-lock.json ./
RUN npm ci

COPY src/TreeEditor.Api/ClientApp/ ./
# vite.config.ts writes the bundle to ../wwwroot
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src

COPY src/TreeEditor.Domain/TreeEditor.Domain.csproj src/TreeEditor.Domain/
COPY src/TreeEditor.Application/TreeEditor.Application.csproj src/TreeEditor.Application/
COPY src/TreeEditor.Infrastructure/TreeEditor.Infrastructure.csproj src/TreeEditor.Infrastructure/
COPY src/TreeEditor.Api/TreeEditor.Api.csproj src/TreeEditor.Api/
RUN dotnet restore src/TreeEditor.Api/TreeEditor.Api.csproj

COPY src/ src/
COPY --from=client-build /src/src/TreeEditor.Api/wwwroot src/TreeEditor.Api/wwwroot
RUN dotnet publish src/TreeEditor.Api/TreeEditor.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=api-build /app/publish ./
USER $APP_UID
ENTRYPOINT ["dotnet", "TreeEditor.Api.dll"]
