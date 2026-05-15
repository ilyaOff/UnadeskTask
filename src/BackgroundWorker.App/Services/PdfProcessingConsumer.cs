using System.Text.Json;

using BackgroundWorker.Core.Services;

using Infrastructure.RabbitMq;

using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using Shared.Models;

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

		await _channel.QueueDeclareAsync(
		   queue: "pdf.error.queue",
		   durable: true,
		   exclusive: false,
		   autoDelete: false,
		   cancellationToken: stoppingToken);

		var consumer = new AsyncEventingBasicConsumer(_channel);
		consumer.ReceivedAsync += async (sender, args) =>
		{
			var deliveryTag = args.DeliveryTag;
			try
			{
				var body = args.Body.ToArray();
				var message = JsonSerializer.Deserialize<PdfProcessingMessage>(body);

				if(message == null)
				{
					_logger.LogError("Failed to deserialize message, sending to DLQ");
					await MoveToDeadLetterQueue(args, "Invalid message format");
					await _channel.BasicNackAsync(deliveryTag, false, false);
					return;
				}

				_logger.LogInformation("Received message for file: {FileId}", message.FileId);

				using var scope = _serviceProvider.CreateScope();
				var processingService = scope.ServiceProvider.GetRequiredService<DocumentProcessingService>();

				await processingService.ProcessDocumentAsync(message, stoppingToken);

				await _channel.BasicAckAsync(args.DeliveryTag, false);
			}
			catch(FileNotFoundException ex)
			{
				_logger.LogWarning(ex, "File not found, moving to DLQ");
				await MoveToDeadLetterQueue(args, ex.Message);
				await _channel.BasicAckAsync(deliveryTag, false);
			}
			catch(Exception ex)
			{
				_logger.LogError(ex, "Error processing message, will retry");

				// Проверяем количество редиливров
				var retryCount = GetRetryCount(args.BasicProperties);

				if(retryCount >= 3)
				{
					_logger.LogError("Max retry count reached, moving to DLQ");
					await MoveToDeadLetterQueue(args, $"Max retries exceeded: {ex.Message}");
					await _channel.BasicAckAsync(deliveryTag, false);
				}
				else
				{
					// Отправляем на повтор с задержкой
					await _channel.BasicNackAsync(deliveryTag, false, true);
				}
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

	private async Task MoveToDeadLetterQueue(BasicDeliverEventArgs args, string errorMessage)
	{
		if(_channel == null)
			return;

		try
		{
			var originalBody = args.Body.ToArray();
			var properties = new BasicProperties
			{
				Persistent = true,
				Headers = new Dictionary<string, object?>
				{
					{ "x-error", errorMessage },
					{ "x-original-routing-key", args.RoutingKey },
					{ "x-timestamp", DateTime.UtcNow.ToString("O") }
				}
			};

			await _channel.BasicPublishAsync(
				exchange: "",
				routingKey: "pdf.error.queue",
				mandatory: true,
				basicProperties: properties,
				body: originalBody);

			_logger.LogInformation("Moved message to DLQ: {ErrorMessage}", errorMessage);
		}
		catch(Exception ex)
		{
			_logger.LogError(ex, "Failed to move message to DLQ");
		}
	}

	private int GetRetryCount(IReadOnlyBasicProperties properties)
	{
		if(properties.Headers != null &&
			properties.Headers.TryGetValue("x-retry-count", out var retryObj))
		{
			return Convert.ToInt32(retryObj);
		}
		return 0;
	}

}