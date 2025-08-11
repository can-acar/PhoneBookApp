#!/bin/bash

# Function to check if Kafka is ready
check_kafka() {
  kafka-topics --bootstrap-server kafka:29092 --list > /dev/null 2>&1
  return $?
}

# Wait for Kafka to be ready
echo "Waiting for Kafka to be ready..."
RETRY_COUNT=0
MAX_RETRIES=30
RETRY_INTERVAL=5

until check_kafka || [ $RETRY_COUNT -eq $MAX_RETRIES ]; do
  echo "Waiting for Kafka to be ready... (Attempt: $((RETRY_COUNT+1))/$MAX_RETRIES)"
  sleep $RETRY_INTERVAL
  ((RETRY_COUNT++))
done

if [ $RETRY_COUNT -eq $MAX_RETRIES ]; then
  echo "Failed to connect to Kafka after $MAX_RETRIES attempts"
  exit 1
fi

echo "Kafka is ready. Creating topics..."

# Create topics
echo "Creating Kafka topics..."
kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists --topic contact-events --partitions 3 --replication-factor 1
kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists --topic report-events --partitions 3 --replication-factor 1
kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists --topic notification-events --partitions 3 --replication-factor 1
kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists --topic report-requests --partitions 3 --replication-factor 1
kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists --topic report-completed --partitions 3 --replication-factor 1
kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists --topic notifications --partitions 3 --replication-factor 1
kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists --topic notification-errors --partitions 3 --replication-factor 1

# Create development topics
echo "Creating development topics..."
kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists --topic report-requests-dev --partitions 3 --replication-factor 1
kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists --topic report-completed-dev --partitions 3 --replication-factor 1
kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists --topic notifications-dev --partitions 3 --replication-factor 1
kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists --topic notification-errors-dev --partitions 3 --replication-factor 1

echo "Topics created successfully"

# List topics to verify
echo "Listing topics:"
kafka-topics --bootstrap-server kafka:29092 --list
