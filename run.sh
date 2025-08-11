#!/bin/bash

# Script to build and run the PhoneBookApp microservices

set -e # Exit on error

# Function to display usage
usage() {
  echo "Usage: $0 [OPTIONS]"
  echo "Options:"
  echo "  --build         Build all services before starting"
  echo "  --infra-only    Start only infrastructure services (PostgreSQL, MongoDB, Kafka)"
  echo "  --down          Stop and remove all containers, networks and volumes"
  echo "  --logs [SERVICE] View logs for all services or specified service"
  echo "  --help          Display this help message"
  exit 1
}

# Default values
BUILD=false
INFRA_ONLY=false
DOWN=false
LOGS=false
LOG_SERVICE="all"

# Parse arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --build)
      BUILD=true
      shift
      ;;
    --infra-only)
      INFRA_ONLY=true
      shift
      ;;
    --down)
      DOWN=true
      shift
      ;;
    --logs)
      LOGS=true
      if [[ $# -gt 1 && ! "$2" =~ ^-- ]]; then
        LOG_SERVICE="$2"
        shift
      fi
      shift
      ;;
    --help)
      usage
      ;;
    *)
      echo "Unknown option: $1"
      usage
      ;;
  esac
done

# Function to check if Docker is running
check_docker() {
  if ! docker info > /dev/null 2>&1; then
    echo "Error: Docker is not running. Please start Docker and try again."
    exit 1
  fi
}

# Function to check if Docker Compose is available
check_docker_compose() {
  if ! docker compose version > /dev/null 2>&1; then
    echo "Error: Docker Compose is not available."
    exit 1
  fi
}

check_docker
check_docker_compose

# Handle down option
if [ "$DOWN" = true ]; then
  echo "Stopping and removing containers, networks, and volumes..."
  docker compose down -v
  echo "Done!"
  exit 0
fi

# Handle logs option
if [ "$LOGS" = true ]; then
  if [ "$LOG_SERVICE" = "all" ]; then
    echo "Showing logs for all services..."
    docker compose logs -f
  else
    echo "Showing logs for $LOG_SERVICE..."
    docker compose logs -f "$LOG_SERVICE"
  fi
  exit 0
fi

# Handle build option
if [ "$BUILD" = true ]; then
  echo "Building services..."
  if [ "$INFRA_ONLY" = true ]; then
    echo "Skipping build for application services as --infra-only was specified"
  else
    docker compose build
  fi
fi

# Start services
if [ "$INFRA_ONLY" = true ]; then
  echo "Starting only infrastructure services..."
  docker compose up -d postgres mongodb zookeeper kafka kafka-ui
else
  echo "Starting all services..."
  docker compose up -d
fi

echo "Waiting for services to be ready..."
sleep 5

# Check service health
if [ "$INFRA_ONLY" = false ]; then
  echo "Checking service health..."
  
  # Define services to check
  SERVICES=(
    "contactservice.api:7001"
    "reportservice.api:7002"
    "notificationservice.api:7003"
  )
  
  # Check each service
  for SERVICE_PORT in "${SERVICES[@]}"; do
    IFS=: read -r SERVICE PORT <<< "$SERVICE_PORT"
    echo "Checking $SERVICE at port $PORT..."
    
    RETRY_COUNT=0
    MAX_RETRIES=10
    RETRY_INTERVAL=3
    
    while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
      if curl -s "http://localhost:$PORT/health" > /dev/null; then
        echo "$SERVICE is healthy!"
        break
      else
        echo "$SERVICE is not ready yet. Retrying in $RETRY_INTERVAL seconds..."
        sleep $RETRY_INTERVAL
        ((RETRY_COUNT++))
      fi
    done
    
    if [ $RETRY_COUNT -eq $MAX_RETRIES ]; then
      echo "Warning: $SERVICE may not be healthy. Check logs with './run.sh --logs $SERVICE'"
    fi
  done
fi

# Print service URLs
echo ""
echo "==== Service URLs ===="
echo "Contact Service API: http://localhost:7001"
echo "Report Service API: http://localhost:7002"
echo "Notification Service API: http://localhost:7003"
echo "Kafka UI: http://localhost:8080"
echo ""
echo "To view logs: ./run.sh --logs [service_name]"
echo "To stop all services: ./run.sh --down"
