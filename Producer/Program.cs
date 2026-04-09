using System.Text;
using RabbitMQ.Client;

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.ExchangeDeclareAsync(exchange: "logs", type: ExchangeType.Fanout);

while (true)
{
    Console.WriteLine("Enter message to send (or 'exit' to quit):");
    var message = Console.ReadLine();
    if (string.IsNullOrEmpty(message)) continue;
    if (message.ToLower() == "exit") break;

    var body = Encoding.UTF8.GetBytes(message);
    await channel.BasicPublishAsync(
        exchange: "logs",
        routingKey: string.Empty,
        body: body);

    Console.WriteLine($" [x] Sent {message}");
}
