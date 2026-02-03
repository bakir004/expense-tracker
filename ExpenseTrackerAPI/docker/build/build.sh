#!/bin/bash

# Build script for the Expense Tracker API Docker image
# Run from repo root: ./ExpenseTrackerAPI/docker/build/build.sh
# Or from ExpenseTrackerAPI: ./docker/build/build.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# ExpenseTrackerAPI dir is one level up from docker/build/
EXPENSE_TRACKER_API_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
# Repo root is one level up from ExpenseTrackerAPI
PROJECT_ROOT="$(cd "${EXPENSE_TRACKER_API_ROOT}/.." && pwd)"

cd "${PROJECT_ROOT}"

IMAGE_NAME="${IMAGE_NAME:-bakeroni1/expense-tracker-api}"
IMAGE_TAG="${IMAGE_TAG:-latest}"

echo "Building Docker image: ${IMAGE_NAME}:${IMAGE_TAG}"
echo "Dockerfile: ExpenseTrackerAPI/docker/build/Dockerfile"
echo "Build context: ExpenseTrackerAPI/"

docker build \
  -f ExpenseTrackerAPI/docker/build/Dockerfile \
  -t "${IMAGE_NAME}:${IMAGE_TAG}" \
  ExpenseTrackerAPI

echo "Build complete: ${IMAGE_NAME}:${IMAGE_TAG}"
