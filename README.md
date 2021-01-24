# AmazonSqsQueue

In order to make use of the `AmazonSqsQueue` you will need access to an AWS account or [use a local development implementation](https://stackoverflow.com/questions/2336438/emulating-amazon-sqs-during-development).

You may want to take a look at [Messaging Using Amazon SQS](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/sqs-apis-intro.html).

## Configuration

The queue configuration is part of the specified uri, e.g.:

``` xml
<inbox
    workQueueUri="amazonsqs://endpoint-name/queue-name?maxMessages=15"
    .
    .
    .
/>
```

| Segment / Argument | Default | Description |
| --- | --- | --- | 
| endpoint-name | required | Will be resolved by an `IAmazonSqsConfiguration` implementation (*see below*). |
| queue-name | required | The name of queue to connect to. |
| maxMessages | 1 | Specifies the number of messages to fetch from the queue. |

## IAmazonSqsConfiguration

```c#
AmazonSQSConfig GetConfiguration(string endpointName);
```

Should return the `AmazonSQSConfig` to use with the `AmazonSQSClient`.

## DefaultAmazonSqsConfiguration

The `DefaultAmazonSqsConfiguration` instance implementing the `IAmazonSqsConfiguration` interface will be registered using the [container bootstrapping](http://shuttle.github.io/shuttle-core/overview-container/#Bootstrapping).  If you wish to override the configuration you should register your instance before calling the `ServiceBus.Register()` method.

This implementation will return the relevant `endpoint` element for the specified `ewndpoint-name`:

```xml
<?xml version="1.0" encoding="utf-8"?>

<configuration>
  <configSections>
    <section name="amazonSqs" type="Shuttle.Esb.AmazonSqsSection, Shuttle.Esb.AmazonSqs" />
  </configSections>

  <amazonSqs>
    <endpoints>
      <endpoint 
        name="local"
        serviceUrl="http://localhost:9324"
      />
    </endpoints>
  </amazonSqs>
</configuration>
```
