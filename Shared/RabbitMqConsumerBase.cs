using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Shared;

public abstract class RabbitMqConsumerBase
{
    private readonly string _consumerName;
    private readonly string _exchangeName;

    protected RabbitMqConsumerBase(string consumerName, string exchangeName = "logs")
    {
        _consumerName = consumerName;
        _exchangeName = exchangeName;
    }

    public async Task StartAsync()
    {
        Console.Title = _consumerName;
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(exchange: _exchangeName, type: ExchangeType.Fanout);

        var queueDeclareResult = await channel.QueueDeclareAsync();
        var queueName = queueDeclareResult.QueueName;

        await channel.QueueBindAsync(queue: queueName, exchange: _exchangeName, routingKey: string.Empty);

        Console.WriteLine($" [*] {_consumerName} waiting for logs.");

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            await HandleMessageAsync(message);
        };

        await channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }

    protected abstract Task HandleMessageAsync(string message);
}
