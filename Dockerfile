# syntax=docker/dockerfile:1

FROM node:22-alpine AS frontend-build
WORKDIR /src/FrontEnd

COPY FrontEnd/package*.json ./
RUN npm ci

COPY FrontEnd/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet-build
WORKDIR /src

COPY BusinessLogicLayer/BusinessLogicLayer.csproj BusinessLogicLayer/
COPY DataAccessLayer/DataAccessLayer.csproj DataAccessLayer/
COPY GF3.WebApi/WebApi.csproj GF3.WebApi/
RUN dotnet restore GF3.WebApi/WebApi.csproj

COPY BusinessLogicLayer/ BusinessLogicLayer/
COPY DataAccessLayer/ DataAccessLayer/
COPY GF3.WebApi/ GF3.WebApi/
RUN dotnet publish GF3.WebApi/WebApi.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false \
    /p:SkipFrontendBuild=true

COPY --from=frontend-build /src/FrontEnd/dist/ /app/publish/wwwroot/

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

RUN mkdir -p /data && chown -R app:app /data /app

COPY --from=dotnet-build --chown=app:app /app/publish/ ./

USER app
EXPOSE 8080

ENTRYPOINT ["dotnet", "WebApi.dll"]
