using Shared;

var consumer = new Consumer2App();
await consumer.StartAsync();

class Consumer2App : RabbitMqConsumerBase
{
    public Consumer2App() : base("Consumer 2") { }

    protected override Task HandleMessageAsync(string message)
    {
        Console.WriteLine($" [x] Consumer 2 Received: {message}");
        return Task.CompletedTask;
    }
}
