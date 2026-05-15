using System.Text.Json;

using BackgroundWorker.Core.Services;

using Infrastructure.RabbitMq;

using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using Shared.Models;
using Shared.RabbitMq;

namespace BackgroundWorker.App.Services;

public class PdfProcessingConsumer : BackgroundService
{
	private readonly IRabbitMqConnection _connection;
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<PdfProcessingConsumer> _logger;
	private readonly RabbitMqSettings _settings;
	private IChannel? _channel;

	public PdfProcessingConsumer(
		IRabbitMqConnection connection,
		IServiceProvider serviceProvider,
		IOptions<RabbitMqSettings> settings,
		ILogger<PdfProcessingConsumer> logger)
	{
		_connection = connection;
		_serviceProvider = serviceProvider;
		_logger = logger;
		_settings = settings.Value;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_channel = await _connection.GetChannelAsync();

		await _channel.QueueDeclareAsync(
			queue: _settings.PdfProcessingQueue,
			durable: true,
			exclusive: false,
			autoDelete: false,
			cancellationToken: stoppingToken);

		var consumer = new AsyncEventingBasicConsumer(_channel);
		consumer.ReceivedAsync += async (sender, args) =>
		{
			try
			{
				var body = args.Body.ToArray();
				var message = JsonSerializer.Deserialize<PdfProcessingMessage>(body);

				if(message == null)
				{
					_logger.LogError("Failed to deserialize message");
					await _channel.BasicNackAsync(args.DeliveryTag, false, false);
					return;
				}

				_logger.LogInformation("Received message for file: {FileId}", message.FileId);

				using var scope = _serviceProvider.CreateScope();
				var processingService = scope.ServiceProvider.GetRequiredService<DocumentProcessingService>();

				await processingService.ProcessDocumentAsync(message, stoppingToken);

				await _channel.BasicAckAsync(args.DeliveryTag, false);
			}
			catch(Exception ex)
			{
				_logger.LogError(ex, "Error processing message");
				await _channel.BasicNackAsync(args.DeliveryTag, false, true); // requeue
			}
		};

		await _channel.BasicConsumeAsync(
			queue: _settings.PdfProcessingQueue,
			autoAck: false,
			consumer: consumer,
			cancellationToken: stoppingToken);

		// Ждем отмены
		await Task.Delay(Timeout.Infinite, stoppingToken);
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		if(_channel != null)
		{
			await _channel.CloseAsync(cancellationToken);
		}
		await base.StopAsync(cancellationToken);
	}
}