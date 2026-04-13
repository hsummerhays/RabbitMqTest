# RabbitMQ .NET 10 Project Walkthrough

The project demonstrates a classic **Publisher/Subscriber (Pub/Sub)** pattern using RabbitMQ's **Fanout Exchange**. This architecture allows a single producer to broadcast messages to multiple consumers simultaneously.

## 1. Architecture Overview
- **Producer**: A console app that sends messages to an exchange.
- **Exchange (`logs`)**: Configured as a `Fanout` exchange, which behaves like a broadcaster. It routes any message it receives to all queues bound to it.
- **Consumers** (`Consumer1`, `Consumer2`, `Consumer3`): Three console apps that each create their own temporary, anonymous queue and bind it to the exchange to listen for messages.
- **Shared Library**: Contains base logic for consumers to avoid repeating the connection and setup code.

Let's break down the code:

---

## 2. The Shared Base Class (`Shared/RabbitMqConsumerBase.cs`)
This class abstracts away the boilerplate code needed to connect to RabbitMQ and start listening for messages, making your consumers extremely lightweight.

```csharp
public async Task StartAsync()
{
    Console.Title = _consumerName;
    // 1. Establish connection to local RabbitMQ server
    var factory = new ConnectionFactory { HostName = "localhost" };
    using var connection = await factory.CreateConnectionAsync();
    using var channel = await connection.CreateChannelAsync();

    // 2. Declare the Fanout Exchange ("logs" by default)
    // Fanout distributes messages to all bound queues blindly.
    await channel.ExchangeDeclareAsync(exchange: _exchangeName, type: ExchangeType.Fanout);

    // 3. Declare a non-durable, exclusive, auto-delete queue with a server-generated name.
    var queueDeclareResult = await channel.QueueDeclareAsync();
    var queueName = queueDeclareResult.QueueName;

    // 4. Bind the generated queue to the exchange
    await channel.QueueBindAsync(queue: queueName, exchange: _exchangeName, routingKey: string.Empty);

    Console.WriteLine($" [*] {_consumerName} waiting for logs.");

    // 5. Create the consumer that listens to the queue
    var consumer = new AsyncEventingBasicConsumer(channel);
    consumer.ReceivedAsync += async (model, ea) =>
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        
        // Push the processing to the concrete implementation
        await HandleMessageAsync(message); 
    };

    // 6. Start consuming
    await channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);

    // Block the thread so it keeps listening
    Console.WriteLine(" Press [enter] to exit.");
    Console.ReadLine();
}

// Concrete consumers must implement this to define their specific handling logic
protected abstract Task HandleMessageAsync(string message);
```

---

## 3. The Consumer Implementation (`Consumer1/Program.cs`)
Because all the heavy lifting is handled by the `RabbitMqConsumerBase`, individual consumers just need to inherit from the base class and define what happens when a message is received.

```csharp
using Shared;

var consumer = new Consumer1App();
await consumer.StartAsync(); // Kicks off the base class connection/binding workflow

class Consumer1App : RabbitMqConsumerBase
{
    // Pass the name to the base class so the console title updates correctly
    public Consumer1App() : base("Consumer 1") { }

    // Implement the specific text to output when a message is caught
    protected override Task HandleMessageAsync(string message)
    {
        Console.WriteLine($" [x] Consumer 1 Received: {message}");
        return Task.CompletedTask;
    }
}
```
*(Consumers 2 and 3 are functionally identical, just with different naming).*

---

## 4. The Producer (`Producer/Program.cs`)
The producer does not concern itself with queues at all. It just connects to RabbitMQ, ensures the exchange exists, and pushes messages directly to it.

```csharp
using System.Text;
using RabbitMQ.Client;

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

// 1. Declare the exchange in case the producer starts before the consumers
await channel.ExchangeDeclareAsync(exchange: "logs", type: ExchangeType.Fanout);

// 2. Loop continuously to accept user input
while (true)
{
    Console.WriteLine("Enter message to send (or 'exit' to quit):");
    var message = Console.ReadLine();
    if (string.IsNullOrEmpty(message)) continue;
    if (message.ToLower() == "exit") break;

    var body = Encoding.UTF8.GetBytes(message);
    
    // 3. Publish the message explicitly *to the exchange*, NOT to a targeted queue.
    // Fanout ignores routing keys, so string.Empty is used.
    await channel.BasicPublishAsync(
        exchange: "logs",
        routingKey: string.Empty, 
        body: body);

    Console.WriteLine($" [x] Sent {message}");
}
```

## Summary of Workflow
1. You run `Start-Environment.ps1`.
2. A RabbitMQ Docker container spins up locally.
3. The Consumers start. They reach out to RabbitMQ, create the `logs` exchange, generate temporary queues mapped to themselves, and bind them to the exchange. They sit and wait.
4. The Producer starts. It accepts a string from the user, encodes it as bytes, and fires it at the `logs` exchange.
5. RabbitMQ acts as the post office—its `logs` Exchange receives the message, looks at its bindings, and pushes identical copies of the message to Consumer 1, Consumer 2, and Consumer 3's queues.

---

## 5. Potential Enhancements (Production Readiness)
While the current design is great for learning the basics of RabbitMQ routing, a production-grade implementation would require a few structural and reliability enhancements.

### A. Dependency Injection & BackgroundService
Instead of manually creating connection factories in simple console scripts, the consumers should be refactored into **Worker Services**.
- Register the RabbitMQ `IConnection` as a **Singleton** in the DI container (since connections are expensive).
- Have consumers inherit from `BackgroundService` to leverage structured logging (`ILogger`), proper application lifecycles, and `CancellationToken` for graceful shutdowns.

### B. Durable Queues & Persistent Messages (Broker Resilience)
Currently, a RabbitMQ server restart will wipe out our dynamic queues and any messages flying through the system.
- **Durable Queues**: Declare the queue with `durable: true` so the queue itself survives a broker crash.
- **Persistent Messages**: On the Producer side, attach `BasicProperties` with `DeliveryMode = DeliveryMode.Persistent` so the message is actively saved to the broker's disk.

### C. Manual Acknowledgements & QoS (Consumer Resilience)
Currently, we use `autoAck: true`, meaning RabbitMQ immediately deletes messages once they hit the network layer. If a consumer crashes while processing, the message is permanently lost.
- **BasicAck (Receipt)**: Turn on `autoAck: false` and explicitly call `channel.BasicAckAsync(ea.DeliveryTag, false)` *only after* a message is successfully processed. If an exception occurs, `channel.BasicNackAsync` can be used to requeue it.
- **BasicQos (Speed Limit)**: Configure `channel.BasicQosAsync(0, 1, false)` to ensure RabbitMQ only feeds 1 unacknowledged message to a consumer at a time, preventing worker overload and ensuring fair dispatching.

### D. JSON Message Serialization
Currently, the system passes raw strings (`Encoding.UTF8.GetBytes()`), but standard practice dictates passing complex entities.
- Serialize/Deserialize objects using `System.Text.Json`.
- Always populate `BasicProperties.ContentType` (e.g., `"application/json"`) to ensure consumers know exactly how to parse the incoming byte stream.

### E. Retry Handling & Dead-Letter Queues (DLQs)
If `autoAck` is disabled and `BasicNack(requeue: true)` is called on an exception, a consumer can become stuck in an infinite loop failing on the same corrupt message.
- **Polly Retries**: Wrap message processing in a configured retry policy (e.g., attempt 3 times with exponential backoff).
- **Dead-Letter Exchanges (DLX)**: Configure the queue with `x-dead-letter-exchange` so that permanently failed messages are safely routed to a graveyard queue for engineering review, keeping the primary workflow unblocked.

### F. Separation of Publisher/Consumer Abstractions
Directly instantiating `ConnectionFactory` violates single-responsibility principles in complex applications.
- **Publisher**: The logic for formatting and sending AMQP messages should be decoupled behind an interface like `IMessagePublisher.PublishAsync<T>(T message)`, abstracting it entirely from web controllers or command handlers.
- **Consumer**: Moving away from the `RabbitMqConsumerBase` class directly managing the connection, you should cleanly divide the RabbitMQ broker integration (Host mapping) from the actual Domain logic.
