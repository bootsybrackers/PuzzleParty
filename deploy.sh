#!/bin/bash
set -e

IMAGE="bootsybrackers/puzzleparty-server"
VPS_USER="administrator"
VPS_HOST="slamdunkinteractive.com"

# Usage: ./deploy.sh <env> <version>
ENV=$1
VERSION=$2

if [ -z "$ENV" ] || [ -z "$VERSION" ]; then
  echo "Error: Missing arguments. Usage: ./deploy.sh <env> <version> (e.g. ./deploy.sh stage 1.0.3)"
  exit 1
fi

TAG="v$VERSION"

if [ "$ENV" = "prod" ]; then
  CONTAINER_NAME="puzzleparty-server-prod"
  DB_NAME="PuzzleParty"
  PORT=8081
  URL="https://pp.slamdunkinteractive.com"
elif [ "$ENV" = "stage" ]; then
  CONTAINER_NAME="puzzleparty-server-stage"
  DB_NAME="PuzzleParty-Stage"
  PORT=8082
  URL="https://stage.pp.slamdunkinteractive.com"
else
  echo "Error: Unknown environment '$ENV'. Use 'prod' or 'stage'."
  exit 1
fi

if [ -z "$MONGODB_CONNECTION_STRING" ]; then
  echo "Error: MONGODB_CONNECTION_STRING environment variable is not set."
  exit 1
fi

# Extra confirmation for prod
if [ "$ENV" = "prod" ]; then
  read -p "==> You are deploying $TAG to PRODUCTION. Are you sure? (yes/no): " CONFIRM
  if [ "$CONFIRM" != "yes" ]; then
    echo "Aborted."
    exit 1
  fi
fi

echo "==> Deploying $IMAGE:$TAG to $ENV ($URL)..."

ssh $VPS_USER@$VPS_HOST << EOF
  echo "Pulling $IMAGE:$TAG..."
  sudo docker pull $IMAGE:$TAG

  echo "Stopping old container..."
  sudo docker stop $CONTAINER_NAME || true
  sudo docker rm $CONTAINER_NAME || true

  echo "Starting new container..."
  sudo docker run -d --name $CONTAINER_NAME \
    --restart unless-stopped \
    -p $PORT:8081 \
    -e MongoDB__ConnectionString="$MONGODB_CONNECTION_STRING" \
    -e MongoDB__DatabaseName="$DB_NAME" \
    -e APP_VERSION="$TAG" \
    $IMAGE:$TAG

  echo "Cleaning up old images..."
  sudo docker image prune -f
EOF

echo "==> Done! $IMAGE:$TAG is live at $URL"
