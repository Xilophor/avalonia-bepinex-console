using System;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using Microsoft.Extensions.Hosting;
using NetMQ;
using NetMQ.Sockets;
using OdinSerializer;
using Sigurd.AvaloniaBepInExConsole.Common;

namespace Sigurd.AvaloniaBepInExConsole.App.Logs;

public class BepInExLogListener : BackgroundService, ILogListener
{
    public SourceList<LogEvent> LogMessages { get; } = new();

    public BepInExLogLevel LogLevel { get; set; } = BepInExLogLevel.Debug;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var runtime = new NetMQRuntime();
        runtime.Run(stoppingToken, ReceiveMessagesAsync(stoppingToken));
        return Task.CompletedTask;
    }

    async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Starting subscriber");
        using var subscriber = new SubscriberSocket("@tcp://localhost:38554");

        subscriber.Subscribe("logMessage");
        subscriber.Subscribe("gameLifetime");

        while (!cancellationToken.IsCancellationRequested) {
            try {
                var (topic, more) = await subscriber.ReceiveFrameStringAsync(cancellationToken);

                switch (topic) {
                    case "logMessage":
                        if (!more) continue;
                        await ReceiveLogMessageAsync(subscriber);
                        break;
                    case "gameLifetime":
                        if (!more) continue;
                        await ReceiveGameLifetimeEventAsync(subscriber);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(topic));
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception exc) {
                Console.WriteLine(exc);
            }
        }
    }

    async Task ReceiveGameLifetimeEventAsync(SubscriberSocket subscriber)
    {
        var payload = await subscriber.ReceiveMultipartMessageAsync();
        var gameLifetimeEvent = SerializationUtility.DeserializeValue<GameLifetimeEvent>(payload.First.Buffer, DataFormat.Binary);
        switch (gameLifetimeEvent.Type) {
            case GameLifetimeEventType.Start:
#if DEBUGAPP
                Console.WriteLine("Received start event, clearing log");
#endif
                LogMessages.Clear();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    async Task ReceiveLogMessageAsync(SubscriberSocket subscriber)
    {
        var payload = await subscriber.ReceiveMultipartMessageAsync();
        var logEvent = SerializationUtility.DeserializeValue<LogEvent>(payload.First.Buffer, DataFormat.Binary);
#if DEBUGAPP
        Console.WriteLine($"Received log message {logEvent}");
#endif
        if (logEvent.Level < LogLevel) return;
        if (logEvent.AnsiFormattedContent.Contains("[Warning: Unity Log] BoxColliders does not support nega")) return;

        LogMessages.Add(logEvent);
    }
}
