# RabbitMQ .NET 10 Practice App

This project demonstrates a simple Publisher/Subscriber message queue architecture using **RabbitMQ** and **.NET 10**. It contains a single Producer that broadcasts messages to multiple Consumers simultaneously using a `fanout` exchange.

## Architecture

* **Producer**: A console app that reads user input and publishes messages to the `logs` exchange.
* **Consumers** (`Consumer1`, `Consumer2`, `Consumer3`): Three identical console applications that each bind their own anonymous queues to the `logs` exchange. By utilizing the fanout type, every active consumer receives a copy of the broadcasted message.
* **Shared**: A class library containing the `RabbitMqConsumerBase`. This abstract base class encapsulates all the boilerplate RabbitMQ connection, channel, and queue-binding logic allowing the consumers to stay extremely lightweight by simply overriding a `HandleMessageAsync` method.

## Prerequisites

1. [.NET 10 SDK](https://dotnet.microsoft.com/)
2. [RabbitMQ](https://www.rabbitmq.com/download.html) running locally on default ports. 
   * The easiest way is via Docker:
     ```bash
     docker run -d --hostname my-rabbit --name some-rabbit -p 5672:5672 -p 15672:15672 rabbitmq:3-management
     ```

## How to Run

### Automated Multi-Pane Launch (Recommended)
You can launch the entire ecosystem concurrently utilizing Windows Terminal! Instead of opening multiple tabs manually, simply run our powershell script securely:

```powershell
.\Start-Environment.ps1
```
This script handles building the solution sequentially safely, then opens a brand new, visually organized multi-pane Windows Terminal containing:
- Bottom-Left Pane: A dynamically managed Docker container initializing RabbitMQ.
- Top-Left Pane: The Producer queueing messages.
- Triple Right Panes: Consumers 1, 2, and 3 actively listening on the Fanout Exchange. 

### Manual Execution
If not using the powershell script, you can run them manually:
1. Start local RabbitMQ container.
2. In separate terminals run the Consumer apps: `dotnet run --project ConsumerX/ConsumerX.csproj`
3. In a final terminal, run the Producer: `dotnet run --project Producer/Producer.csproj`

## Acknowledgements
This project is currently a **Work In Progress**.
*This repository was made possible and drastically simplified by AI, specifically acknowledging the influence of Google Gravity for the extra effort involved in its creation.*

   ```bash
   dotnet run --project Consumer1/Consumer1.csproj
   ```
   ```bash
   dotnet run --project Consumer2/Consumer2.csproj
   ```
   ```bash
   dotnet run --project Consumer3/Consumer3.csproj
   ```

2. **Start the Producer:**
   Open a final terminal and run:

   ```bash
   dotnet run --project Producer/Producer.csproj
   ```

3. **Send Messages!**
   Type any message into the terminal running the Producer and hit `Enter`. Watch the message instantly appear in all of the consumer terminals! Type `exit` in the producer to close the loop.
