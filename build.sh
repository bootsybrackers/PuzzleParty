#!/bin/bash
set -e

SERVER_IMAGE="bootsybrackers/puzzleparty-server"
WEB_IMAGE="bootsybrackers/puzzleparty-web"

# Usage: ./build.sh <version> [server|web|all]
VERSION=$1
TARGET=${2:-all}

if [ -z "$VERSION" ]; then
  echo "Error: No version specified. Usage: ./build.sh <version> [server|web|all]"
  echo "  e.g. ./build.sh 1.0.3"
  echo "  e.g. ./build.sh 1.0.3 server"
  echo "  e.g. ./build.sh 1.0.3 web"
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

if [ "$TARGET" = "server" ] || [ "$TARGET" = "all" ]; then
  echo "==> Building $SERVER_IMAGE:$TAG for linux/amd64..."
  cd Server
  docker buildx build --platform linux/amd64 \
    -t $SERVER_IMAGE:$TAG \
    -t $SERVER_IMAGE:latest \
    --push \
    .
  cd ..
fi

if [ "$TARGET" = "web" ] || [ "$TARGET" = "all" ]; then
  echo "==> Building $WEB_IMAGE:$TAG for linux/amd64..."
  cd Web
  docker buildx build --platform linux/amd64 \
    -t $WEB_IMAGE:$TAG \
    -t $WEB_IMAGE:latest \
    --push \
    .
  cd ..
fi

echo "==> Tagging git commit as $TAG..."
git tag $TAG
git push origin $TAG

echo "==> Done! Version $TAG pushed to Docker Hub."
