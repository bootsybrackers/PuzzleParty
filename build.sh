#!/bin/bash
set -e

IMAGE="bootsybrackers/puzzleparty-server"

# Usage: ./build.sh <version>
VERSION=$1

if [ -z "$VERSION" ]; then
  echo "Error: No version specified. Usage: ./build.sh <version> (e.g. ./build.sh 1.0.3)"
  exit 1
fi

TAG="v$VERSION"

# Check for uncommitted changes to tracked files only
if [ -n "$(git status --porcelain | grep -v '^?? ')" ]; then
  echo "Error: You have uncommitted changes. Commit or stash them before building."
  git status --short | grep -v '^?? '
  exit 1
fi

# Check tag doesn't already exist
if git rev-parse "$TAG" >/dev/null 2>&1; then
  echo "Error: Git tag $TAG already exists."
  exit 1
fi

echo "==> Building $IMAGE:$TAG for linux/amd64..."
cd Server
docker buildx build --platform linux/amd64 \
  -t $IMAGE:$TAG \
  -t $IMAGE:latest \
  --push \
  .
cd ..

echo "==> Tagging git commit as $TAG..."
git tag $TAG
git push origin $TAG

echo "==> Done! Image $IMAGE:$TAG pushed to Docker Hub."
