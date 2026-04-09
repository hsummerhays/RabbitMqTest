using Shared;

var consumer = new Consumer3App();
await consumer.StartAsync();

class Consumer3App : RabbitMqConsumerBase
{
    public Consumer3App() : base("Consumer 3") { }

    protected override Task HandleMessageAsync(string message)
    {
        Console.WriteLine($" [x] Consumer 3 Received: {message}");
        return Task.CompletedTask;
    }
}
