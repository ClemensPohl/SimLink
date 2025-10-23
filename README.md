# SimLink

A simple simulator that exposes CNC machine states through an OPC UA server and publishes telemetry to an MQTT broker. Dockerized services subscribe to the broker to hydrate a Redis database and a Kafka queue. Kafka consumes only machine-state topics and performs context enrichment using data from Redis.

# Startup
Prerequisites
- Docker
- .NET SDK (to run the SimLink project)

1. Create the Docker network if it does not exist:
```powershell
docker network create adm_live_demo
```

2. Start the container services (run from repository root):
```bash
docker compose -f sim-link-container/docker-compose.yaml up -d
```

3. Run the SimLink application (starts the OPC UA server and the MQTT client)

From Visual Studio / IDE:
- Open the SimLink project and run the StartupApp (SimLink).

What to expect
- An OPC UA server exposing simulated CNC machines.
- A HiveMQ MQTT client that publishes machine telemetry to the broker.

# MQTT-Topic Design
```
pohl-industries/
  {plant}/
    machines/
      {serial-number}/
        telemetry/
          spindleSpeed
          OrderNumber
          ...
        command/
          +StartMachine()
          +StopMachine()
          ...
```

## Configuration files
- Application settings: [SimLink/appsettings.json](SimLink/appsettings.json)
- Docker compose: [sim-link-container/docker-compose.yaml](sim-link-container/docker-compose.yaml)
- Redis hydration config: [sim-link-container/redis_hydration.yaml](sim-link-container/redis_hydration.yaml)
- Kafka hydration config: [sim-link-container/kafka_hydration.yaml](sim-link-container/kafka_hydration.yaml)