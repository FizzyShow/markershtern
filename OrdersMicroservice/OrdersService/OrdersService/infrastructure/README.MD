## Environment quick setup
### Prerequisites
* Docker [download](https://www.docker.com/get-started/)
* Docker-compose

### Run
```cmd
docker compose up -d
```


### Connection strings
```md
- Kafka: localhost:9092
- Kowl: localhost:8080
- Mongodb: mongodb://localhost:27017 (primary), (27018, 27019 - ?, не разобрался в них)
```

### Set memory limits for JVM via env vars
- KAFKA_MAX_MEM, default: 1024m

