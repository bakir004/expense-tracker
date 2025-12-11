# This dockerfile expects the working directory to be project-root

# Specify docker registry, and base and runtime images
ARG BASE_REGISTRY="mcr.microsoft.com"
ARG BUILD_IMAGE="dotnet/sdk:8.0"
ARG RUNTIME_IMAGE="dotnet/aspnet:8.0"

# Specify which user runs the build container (default root)
ARG BUILD_CHOWN="root:root"

# Application specific arguments
ARG APP_NAME_ARG="SampleCkWebApp"
ARG DOTNET_CONFIGURATION="Release"
ARG DOTNET_BUILD_PROPS=""

# Docker labels
ARG BUILD_DATE
ARG COMMIT_HASH
ARG VCS_CLEAN

# STAGE 1 - Create final docker image base image
FROM ${BASE_REGISTRY}/${RUNTIME_IMAGE} AS base
ARG APP_NAME_ARG
ARG DOTNET_CONFIGURATION
ARG DOTNET_BUILD_PROPS
ARG BUILD_DATE
ARG COMMIT_HASH
ARG VCS_CLEAN
WORKDIR /app
EXPOSE 8080
LABEL org.opencontainers.image.title=${APP_NAME_ARG}
LABEL org.opencontainers.image.revision=${COMMIT_HASH}
LABEL org.opencontainers.image.created=${BUILD_DATE}

# STAGE 2 - Create and set up separate container image for building from source
FROM ${BASE_REGISTRY}/${BUILD_IMAGE} AS build
ARG APP_NAME_ARG
ARG DOTNET_CONFIGURATION
ARG DOTNET_BUILD_PROPS
ARG BUILD_CHOWN
ENV APP_NAME=$APP_NAME_ARG
ENV CC_REPO_ROOT="."
ENV APP_SRC_ROOT="${CC_REPO_ROOT}/${APP_NAME}/src"
# Copy all csproj files
WORKDIR /build
COPY --chown=${BUILD_CHOWN} ["Directory.Build.props", "."]
COPY --chown=${BUILD_CHOWN} ["${CC_REPO_ROOT}/nuget.config", "${CC_REPO_ROOT}/."]
COPY --chown=${BUILD_CHOWN} ["${CC_REPO_ROOT}/Directory.Build.props", "${CC_REPO_ROOT}/."]
COPY --chown=${BUILD_CHOWN} ["${CC_REPO_ROOT}/${APP_NAME}/", "${CC_REPO_ROOT}/${APP_NAME}/"]

# Restore NuGet packages
RUN dotnet restore --ignore-failed-sources \
    -p:Configuration=${DOTNET_CONFIGURATION} \
    --configfile "${CC_REPO_ROOT}/nuget.config" \
    "${APP_SRC_ROOT}/${APP_NAME}.WebApi/${APP_NAME}.WebApi.csproj"
# END STAGE 2
    
# STAGE 3 - Use 'build' container from stage 2 and build source code
# Switch working directory
WORKDIR "/build/${APP_SRC_ROOT}/${APP_NAME}.WebApi"
# Build target csproj and save output in /app/build dir
RUN dotnet build "${APP_NAME}.WebApi.csproj" \
    -c ${DOTNET_CONFIGURATION} \
    ${DOTNET_BUILD_PROPS} \
    -o /app/build --no-restore
# END STAGE 3

# STAGE 4 - Creates final DLLs and publishes contents to /app/publish dir
FROM build AS publish
RUN dotnet publish "${APP_NAME}.WebApi.csproj" \
    -c ${DOTNET_CONFIGURATION} \
    ${DOTNET_BUILD_PROPS} \
    -o /app/publish /p:UseAppHost=false --no-restore
# END STAGE 4

# STAGE 5 - Copy from /app/publish in build container to /app in final container
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "SampleCkWebApp.WebApi.dll"]
# END STAGE 5