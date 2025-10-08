# SimLink

# Startup

1) make sure to create the network if not already
```
docker network create adm_live_demo
```
2) run docker container:
```
docker compose up -d
```
3) run StartupApp "SimLink" as c# project
  -> this will run the ocpua server, create a hivemq client instance and publish opcua state

## docker compose file

services:
  mqtt:
    image: hivemq/hivemq-edge:latest
    container_name: mqtt_broker
    restart: unless-stopped
    ports:
      - "1883:1883"  # MQTT default port
      - "8080:8080"  # HiveMQ Control Center port
      - "8883:8883"  # MQTT over SSL port
    volumes:
      - ./hivemq-data:/opt/hivemq/data
    networks:
      - advanced_data_management_live_demo
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

  redis:
    image: redis/redis-stack:latest
    container_name: redis_container
    restart: unless-stopped
    ports:
      - "6379:6379"  # Redis default port
      - "8001:8001"  # RedisInsight port
    volumes:
      - ./redis-data:/data
    networks:
      - advanced_data_management_live_demo
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

  redis_hydration:
    image: docker.redpanda.com/redpandadata/connect:latest
    container_name: redis_hydration
    restart: unless-stopped
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
    depends_on:
      - mqtt
      - redis
    networks:
      - advanced_data_management_live_demo
    volumes:
      - ./redis_hydration.yaml:/connect.yaml

networks:
  advanced_data_management_live_demo:
    external: true # Use an existing external network


# redis hydration

input:
  label: "mqtt_in"
  mqtt:
    urls:
      - "mqtt://mqtt_broker:1883"
    client_id: "redis-hydration-18e694e4-6eb7-4298-bc60-f3553b151885"
    keepalive: 60
    clean_session: true
    topics:
      - "uns-v1/Factory A/+/+/+/+"

pipeline:
  processors:
    - mapping: |
        root = this
        root.field = meta("mqtt_topic").split("/").index(-1)
        meta key = meta("mqtt_topic").split("/").4
    - mapping: |
        root = { this.field: this.value.string() }

output:
  label: "redis_out"
  redis_hash:
    url: "redis://redis_container:6379"
    key: ${! meta("key") }
    walk_metadata: false
    walk_json_object: true