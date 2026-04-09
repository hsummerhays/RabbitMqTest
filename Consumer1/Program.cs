using Shared;

var consumer = new Consumer1App();
await consumer.StartAsync();

class Consumer1App : RabbitMqConsumerBase
{
    public Consumer1App() : base("Consumer 1") { }

    protected override Task HandleMessageAsync(string message)
    {
        Console.WriteLine($" [x] Consumer 1 Received: {message}");
        return Task.CompletedTask;
    }
}
