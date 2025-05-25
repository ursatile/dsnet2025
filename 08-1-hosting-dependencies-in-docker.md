---
title: "8.1: Hosting Dependencies with Docker"
layout: module
nav_order: 8.1
summary: >
    In this module, we'll look at Docker, and how you can use it to host dependencies for your .NET applications and microservices
---

## Hosting Dependencies using Docker

### Running RabbitMQ from a Docker image

For the examples in this workshop, we’ve been using a RabbitMQ instance hosted by CloudAMQP. This works great when we need to actually send messages between different systems, but it’s not always ideal. Sometimes, we might need a way to connect to services like RabbitMQ without setting up hosted services in the cloud.

One option would be to install RabbitMQ as a local service - download the installer, and the Erlang runtime which it uses, install it, configure it, set it to run at startup, open the necessary ports. I don’t like that approach. I don’t want unnecessary services running on my machine unless I’m actually using them, and I don’t want to risk RabbitMQ interfering with anything else I’m using.

Instead, we can run RabbitMQ  using **Docker** - a virtualisation system that allow us to run small, highly optimised virtual machines which host specific services.

> The most informative thing anybody ever said to me about Docker was that a Docker image is a “virtual machine intended to run one, single program”. The program starts when the VM starts. When the program ends, the VM shuts down. It’s not a general-purpose server; it’s just enough infrastructure to run one single application.

If you want to run RabbitMQ locally using Docker, this command will download the Docker community image for RabbitMQ and run a container based on that image:

```
docker run -d --hostname rabbitmq --name rabbitmq -p 5672:5672 -p 8080:15672 -e RABBITMQ_DEFAULT_USER=user -e RABBITMQ_DEFAULT_PASS=pass rabbitmq:3-management
```

The `--hostname` parameter is important; ordinarily Docker will assign a randomly-generated hostname, but RabbitMQ uses the hostname internally to manage messages and queues, so we need to specify one (`rabbitmq` in this example)

We're exposing port 15672 on localhost port 8080, and setting a default username and password (`user` / `pass` in this case). This means we can open a browser at http://localhost:8080/ and use these credentials to sign in to the RabbitMQ admin interface.

To connect to this RabbitMQ instance using EasyNetQ, use this connection string:

```
"ConnectionStrings": {
    "AutobarnRabbitMQ": "amqp://user:pass@localhost:5672"
},
```

### Running Graylog in Docker

In the next section, we’re going to look at how to configure the .NET logging infrastructure to send log messages to a centralised logging service. We’re going to use Graylog here. Graylog in turn requires MongoDB and ElasticSearch, so instead of installing and configuring multiple services, we’re going to use Docker to set up a local Graylog instance.

```yaml
# docker-compose.yml to run Mongo, ElasticSearch and Graylog as Docker images
# copy this to docker-compose.yml, then run it with 'docker-compose up'

version: '3'
services:
    # MongoDB: https://hub.docker.com/_/mongo/
    mongo:
      image: mongo:4.2
      networks:
        - graylog
    # Elasticsearch: https://www.elastic.co/guide/en/elasticsearch/reference/7.10/docker.html
    elasticsearch:
      image: docker.elastic.co/elasticsearch/elasticsearch-oss:7.10.2
      environment:
        - http.host=0.0.0.0
        - transport.host=localhost
        - network.host=0.0.0.0
        - "ES_JAVA_OPTS=-Dlog4j2.formatMsgNoLookups=true -Xms512m -Xmx512m"
      ulimits:
        memlock:
          soft: -1
          hard: -1
      deploy:
        resources:
          limits:
            memory: 1g
      networks:
        - graylog
    # Graylog: https://hub.docker.com/r/graylog/graylog/
    graylog:
      image: graylog/graylog:4.2
      environment:
        # CHANGE ME (must be at least 16 characters)!
        - GRAYLOG_PASSWORD_SECRET=somepasswordpepper
        # Password: admin
        - GRAYLOG_ROOT_PASSWORD_SHA2=8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918
        - GRAYLOG_HTTP_EXTERNAL_URI=http://127.0.0.1:9000/
      entrypoint: /usr/bin/tini -- wait-for-it elasticsearch:9200 --  /docker-entrypoint.sh
      networks:
        - graylog
      restart: always
      depends_on:
        - mongo
        - elasticsearch
      ports:
        # Graylog web interface and REST API
        - 9000:9000
        # Syslog TCP
        - 1514:1514
        # Syslog UDP
        - 1514:1514/udp
        # GELF TCP
        - 12201:12201
        # GELF UDP
        - 12201:12201/udp
networks:
    graylog:
      driver: bridge
```

### Configuring a .NET hosted application to use Graylog

Here’s a simple example of a .NET 6.0 hosted application that uses the Serilog provider to capture logging data and send application logs to a Graylog hosted service using the GELF ingestion endpoint, which listens on TCP/UDP on port 12201:

```csharp
// Program.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Sinks.Graylog;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) => {
        services.AddHostedService<ExampleService>();
    })
    .UseSerilog(ConfigureLogger)
    .Build();

host.Run();

static void ConfigureLogger(HostBuilderContext host, LoggerConfiguration log) {
    log.MinimumLevel.Debug();
    log.WriteTo.Console();
    log.WriteTo.Graylog(new GraylogSinkOptions { HostnameOrAddress = "host.docker.internal", Port = 12201 });
    log.Enrich.WithProcessName();
}

public class ExampleService : IHostedService {
    private readonly ILogger<ExampleService> logger;

    public ExampleService(ILogger<ExampleService> logger) {
        this.logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken) {
        logger.LogInformation("Starting example worker service...");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        logger.LogInformation("Stopping example worker service...");
        return Task.CompletedTask;
    }
}
```

> ### Note on configuring Graylog sinks with Docker:
>
> If you’re hosting Graylog in Docker and running your service directly, use this line:
> `log.WriteTo.Graylog(new GraylogSinkOptions { HostnameOrAddress = "localhost", Port = 12201 });`
>
> If you’re hosting Graylog in Docker, and **also running your service in a local Docker image**, use this configuration:
> `log.WriteTo.Graylog(new GraylogSinkOptions { HostnameOrAddress = "host.docker.internal", Port = 12201 });`
>
> If you’re writing to a Graylog instance hosted on a real hostname, use that host name in the Graylog sink options:
>
> `log.WriteTo.Graylog(new GraylogSinkOptions { HostnameOrAddress = "logs.mycompany.com", Port = 12201 });`

