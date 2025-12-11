#!/bin/bash

# command line arguments
app=$1

# Optional flags
aws=false
debug=false
fips=false
nocache=false
tag="build"
output_registry=""
force_build=false

# ANSI Color Codes
# https://gist.github.com/JBlond/2fea43a3049b38287e5e9cefc87b2124
CLEAR='\033[0m'
BLACK='\033[0;30m'
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
WHITE='\033[0;37m'

# Get the directory path of this script
SCRIPT_PATH="$(realpath "$BASH_SOURCE")"
SCRIPT_DIR="$(dirname "$(realpath "$BASH_SOURCE")")"

# Generic function to build docker image
build_generic() {
    app_name=$1
    docker_file=$2
    app_name_lower=${app_name,,}
    
    # Default dotnet configuration and constants
    dotnet_configuration="Release"
    dotnet_props=""

    # Default docker registry and images
    docker_base_registry="mcr.microsoft.com"
    docker_build_image="dotnet/sdk:8.0"
    docker_runtime_image="dotnet/aspnet:8.0"

    # Build container user
    docker_build_chown="root:root"
    
    # Final image name
    final_image_name="${app_name_lower}:${tag}"
    if [ -n "$output_registry" ]; then
        final_image_name="${output_registry}/${final_image_name}"
    fi
    
    # Check for debug configuration flag
    if [ "$debug" = true ]; then
        dotnet_configuration="Debug"
    fi
    
    # Check for AWS flag
    if [ "$aws" = true ]; then
        dotnet_props+=" -p:EnableAws=true"
    fi

    # Check for FIPS flag
    if [ "$fips" = true ]; then
        docker_base_registry="registry1.dso.mil"
        docker_build_image="ironbank/redhat/dotnet-core/dotnet-sdk-8.0-ubi9-slim:latest"
        docker_runtime_image="ironbank/redhat/dotnet-core/aspnetcore-runtime-8.0:latest"
        docker_build_chown="dotnet:dotnet"
    fi
    
    # Check if the vcs is clean (i.e. no uncommitted changes)
    is_vcs_clean=true
    if [[ -n "$(cd ${SCRIPT_DIR} && git status --porcelain)" ]]; then
        is_vcs_clean=false
        if [ "$force_build" = true ]; then
            echo -e "${YELLOW}Uncommitted changes detected. Building with the current state of the repository.${CLEAR}"
            echo ""
        else
            echo -e "${RED}Uncommitted changes detected. Please commit your changes before building the image.${CLEAR}"
            exit 1
        fi
    fi

    # Log the commit hash
    COMMIT_HASH="$(cd ${SCRIPT_DIR} && git rev-parse HEAD)"
    echo -e "Building $(if [ "$is_vcs_clean" = true ]; then echo -e "${GREEN}clean${CLEAR}"; else echo -e "${RED}dirty${CLEAR}"; fi) build from commit: ${CYAN}${COMMIT_HASH}${CLEAR}"
    echo ""
    
    docker_build_flags=""
    if [ "$nocache" = true ]; then
        docker_build_flags="--no-cache"
    fi

    echo "App name:      $app_name"
    echo "Configuration: $dotnet_configuration"
    echo "Docker file:   $docker_file"
    echo "Image name:    $final_image_name"
    echo ""
    
    # Flag to indicate if docker build was interrupted by user (CTRL+C)
    build_interrupted=false
    
    echo "Building docker image..."
    trap 'build_interrupted=true' SIGINT
    docker build $docker_build_flags \
        --build-arg BASE_REGISTRY="${docker_base_registry}" \
        --build-arg BUILD_IMAGE="${docker_build_image}" \
        --build-arg RUNTIME_IMAGE="${docker_runtime_image}" \
        --build-arg BUILD_CHOWN="${docker_build_chown}" \
        --build-arg DOTNET_CONFIGURATION="${dotnet_configuration}" \
        --build-arg DOTNET_BUILD_PROPS="${dotnet_props}" \
        --build-arg BUILD_DATE="$(date -u +'%Y-%m-%dT%H:%M:%SZ')" \
        --build-arg COMMIT_HASH="${COMMIT_HASH}" \
        --build-arg VCS_CLEAN="$is_vcs_clean" \
         -f "${docker_file}" \
         -t "${final_image_name}" \
         .
    docker_build_result=$?

    if [ "$build_interrupted" = true ]; then
        echo -e "${RED}Build interrupted by user${CLEAR}"
        exit 1
    fi
    
    echo ""
    printf '=%.0s' $(seq 1 $(tput cols))
    echo ""
    echo ""

    # Reprint image summary
    echo "App name:      $app_name"
    echo "Configuration: $dotnet_configuration"
    echo "Docker file:   $docker_file"
    echo "Image name:    $final_image_name"

    echo ""
    if [ "$docker_build_result" == 0 ]; then
        echo -e "${GREEN}$app_name build completed successfully${CLEAR}"
    else
        echo -e "${RED}$app_name build failed with status code ${docker_build_result}${CLEAR}"
        exit 1
    fi
    
    echo ""
}

# SampleCkWebApp specific build
build_sample() {
    build_generic "SampleCkWebApp" "./docker/build/SampleCkWebApp.Dockerfile"
}

print_help() {
    echo ""
    echo "Usage: ./scripts/build.sh <app_name> [options]"
    echo ""
    echo "Parameters:"
    echo "app_name:       all, sample"
    echo ""
    echo "Optional Configs:"
    echo "-a, --aws:      Build for AWS. Default is non-AWS environment."
    echo "-d, --debug:    Build with Debug configuration. Default is Release."
    echo "-f, --fips:     Build in FIPS mode using Ironbank RedHat base image. Default is non-FIPS."
    echo "-t, --tag:      Tag to use for the final docker image. Default is 'build'."
    echo "-r, --registry: Registry to use for the final docker image. Default is empty."
    echo "-n, --nocache:  Build the image without using cache. Default is to use cache."
    echo "--force:        Force build the docker image even if there are uncommitted changes."
    echo "-h, --help:     Display this help message."
    echo ""
    echo "NOTE: Make sure to run this from the monorepo root directory."
    echo "Debug configuration will build with all database types supported."
}

print_command_summary() {
    printf "+================================+\n"
    printf "| Command summary:\n"
    printf "+================================+\n"
    printf "| App name:      %-15s\n" "$app"
    printf "| AWS:           %-15s\n" "$aws"
    printf "| Debug:         %-15s\n" "$debug"
    printf "| FIPS:          %-15s\n" "$fips"
    printf "| Registry:      %-15s\n" "$output_registry"
    printf "| Image tag:     %-15s\n" "$tag"
    printf "| No cache:      %-15s\n" "$nocache"
    printf "| Force build:   %-15s\n" "$force_build"
    printf "+================================+\n"
    echo ""
}

check_params() {
    if [ -z "$app" ]; then
        echo -e "${RED}App name cannot be empty${CLEAR}"
        print_help
        exit 1
    fi
}

# Function to check CLI flags
check_flags()
{
    while [ "$1" != "" ]; do
        if [ "$1" = "-a" ] || [ "$1" = "--aws" ]; then
            aws=true
        elif [ "$1" = "-d" ] || [ "$1" = "--debug" ]; then
            debug=true
        elif [ "$1" = "-f" ] || [ "$1" = "--fips" ]; then
            fips=true
        elif [ "$1" = "-n" ] || [ "$1" = "--nocache" ]; then
            nocache=true
        elif [ "$1" = "--force" ]; then
            force_build=true
        elif [ "$1" = "-t" ] || [ "$1" = "--tag" ]; then
            shift
            if [ -z "$1" ]; then
                echo -e "${RED}Tag value cannot be empty${CLEAR}"
                exit 1
            fi
            tag=$1
        elif [ "$1" = "-r" ] || [ "$1" = "--registry" ]; then
            shift
            if [ -z "$1" ]; then
                echo -e "${RED}Registry value cannot be empty${CLEAR}"
                exit 1
            fi
            output_registry=$1
        elif [ "$1" = "-h" ] || [ "$1" = "--help" ]; then
            print_help
            exit
        fi
        shift
    done
}

# Script main which checks flags then calls the appropriate build function based on the requested app
check_flags "$@"
check_params
print_command_summary

if [ "$app" = "sample" ]; then
    build_sample
elif [ "$app" = "all" ]; then
    build_sample
 
elif [ "$app" = "help" ]; then
    print_help
else
    echo -e "${RED}Invalid app name${CLEAR}"
    print_help
    exit 1
fi