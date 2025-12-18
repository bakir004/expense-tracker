#!/bin/bash

# Build script for the Expense Tracker API Docker image
# Can be run from any directory - will automatically change to project root

set -e

# Get the directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# Project root is two levels up from docker/build/
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

# Change to project root
cd "${PROJECT_ROOT}"

IMAGE_NAME="${IMAGE_NAME:-bakeroni1/expense-tracker-api}"
IMAGE_TAG="${IMAGE_TAG:-latest}"

echo "Building Docker image: ${IMAGE_NAME}:${IMAGE_TAG}"
echo "Using Dockerfile: docker/build/Dockerfile"
echo "Build context: ${PROJECT_ROOT}"

docker build \
  -f docker/build/Dockerfile \
  -t "${IMAGE_NAME}:${IMAGE_TAG}" \
  .

echo "Build complete: ${IMAGE_NAME}:${IMAGE_TAG}"

