#!/bin/bash
set -e

SERVER_IMAGE="bootsybrackers/puzzleparty-server"
WEB_IMAGE="bootsybrackers/puzzleparty-web"
VPS_USER="administrator"
VPS_HOST="slamdunkinteractive.com"

# Usage: ./deploy.sh <env> <version> [server|web|all]
ENV=$1
VERSION=$2
TARGET=${3:-all}

if [ -z "$ENV" ] || [ -z "$VERSION" ]; then
  echo "Error: Missing arguments."
  echo "Usage: ./deploy.sh <env> <version> [server|web|all]"
  echo "  e.g. ./deploy.sh stage 1.0.3"
  echo "  e.g. ./deploy.sh prod 1.0.3 server"
  echo "  e.g. ./deploy.sh prod 1.0.3 web"
  exit 1
fi

TAG="v$VERSION"

if [ "$ENV" = "prod" ]; then
  SERVER_CONTAINER="puzzleparty-server-prod"
  WEB_CONTAINER="puzzleparty-web-prod"
  SERVER_PORT=8081
  WEB_PORT=8083
  SERVER_DB="PuzzleParty"
  SERVER_URL="https://pp.slamdunkinteractive.com"
  WEB_URL="https://www.slamdunkinteractive.com"
elif [ "$ENV" = "stage" ]; then
  SERVER_CONTAINER="puzzleparty-server-stage"
  WEB_CONTAINER="puzzleparty-web-stage"
  SERVER_PORT=8082
  WEB_PORT=8084
  SERVER_DB="PuzzleParty-Stage"
  SERVER_URL="https://stage.pp.slamdunkinteractive.com"
  WEB_URL="https://stage.www.slamdunkinteractive.com"
else
  echo "Error: Unknown environment '$ENV'. Use 'prod' or 'stage'."
  exit 1
fi

if [ "$TARGET" = "server" ] || [ "$TARGET" = "all" ]; then
  if [ -z "$MONGODB_CONNECTION_STRING" ]; then
    echo "Error: MONGODB_CONNECTION_STRING environment variable is not set."
    exit 1
  fi
fi

# Extra confirmation for prod
if [ "$ENV" = "prod" ]; then
  read -p "==> You are deploying $TAG to PRODUCTION. Are you sure? (yes/no): " CONFIRM
  if [ "$CONFIRM" != "yes" ]; then
    echo "Aborted."
    exit 1
  fi
fi

echo "==> Deploying $TAG to $ENV..."

ssh $VPS_USER@$VPS_HOST << EOF

  if [ "$TARGET" = "server" ] || [ "$TARGET" = "all" ]; then
    echo "--- Deploying server ---"
    sudo docker pull $SERVER_IMAGE:$TAG
    sudo docker stop $SERVER_CONTAINER || true
    sudo docker rm $SERVER_CONTAINER || true
    sudo docker run -d --name $SERVER_CONTAINER \
      --restart unless-stopped \
      -p $SERVER_PORT:8081 \
      -e MongoDB__ConnectionString="$MONGODB_CONNECTION_STRING" \
      -e MongoDB__DatabaseName="$SERVER_DB" \
      -e APP_VERSION="$TAG" \
      $SERVER_IMAGE:$TAG
    echo "Server live at $SERVER_URL"
  fi

  if [ "$TARGET" = "web" ] || [ "$TARGET" = "all" ]; then
    echo "--- Deploying web ---"
    sudo docker pull $WEB_IMAGE:$TAG
    sudo docker stop $WEB_CONTAINER || true
    sudo docker rm $WEB_CONTAINER || true
    sudo docker run -d --name $WEB_CONTAINER \
      --restart unless-stopped \
      -p $WEB_PORT:80 \
      $WEB_IMAGE:$TAG
    echo "Web live at $WEB_URL"
  fi

  echo "Cleaning up old images..."
  sudo docker image prune -f

EOF

echo "==> Done!"
