# Build stage
FROM mcr.microsoft.com/dotnet/core/sdk:2.2.202-alpine3.9 as build

RUN apk update && apk upgrade --no-cache

COPY src /src
WORKDIR /src

RUN dotnet publish -c Release

# Run stage
FROM mcr.microsoft.com/dotnet/core/runtime:2.2.3-alpine3.9 as run

RUN apk update && apk upgrade --no-cache

EXPOSE 9091/tcp
ENV ASPNETCORE_URLS http://*:9091

ARG ASPNETCORE_ENVIRONMENT
ARG APP_NAME
ARG OBJECT_URL
ARG STORAGE_URL
ARG RULES_URL
ARG INDEXING_URL

ENV ASPNETCORE_ENVIRONMENT ${ASPNETCORE_ENVIRONMENT}
ENV APP_NAME ${APP_NAME}
ENV OBJECT_URL ${OBJECT_URL}
ENV STORAGE_URL ${STORAGE_URL}
ENV RULES_URL ${RULES_URL}
ENV INDEXING_URL ${INDEXING_URL}

COPY --from=build /src/bin/Release/netcoreapp2.2/publish /app
WORKDIR /app

# don't run as root user
RUN chown 1001:0 /app/Foundation.Example.WebUI.dll
RUN chmod g+rwx /app/Foundation.Example.WebUI.dll
USER 1001

ENTRYPOINT [ "dotnet", "Foundation.Example.WebUI.dll" ]

