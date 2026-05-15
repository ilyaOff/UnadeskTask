using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;

using Shared.RabbitMq;

namespace Infrastructure.RabbitMq;

public interface IRabbitMqPublisher
{
	/// <summary>
	/// Публикует сообщение в очередь (fire-and-forget)
	/// </summary>
	Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;

	/// <summary>
	/// Публикует сообщение с указанием конкретной очереди
	/// </summary>
	Task PublishToQueueAsync<T>(T message, string queueName, CancellationToken cancellationToken = default) where T : class;
}

public class RabbitMqPublisher : IRabbitMqPublisher
{
	private readonly IRabbitMqConnection _connection;
	private readonly RabbitMqSettings _settings;
	private readonly ILogger<RabbitMqPublisher> _logger;
	private IChannel? _channel;

	public RabbitMqPublisher(
		IRabbitMqConnection connection,
		IOptions<RabbitMqSettings> settings,
		ILogger<RabbitMqPublisher> logger)
	{
		_connection = connection;
		_settings = settings.Value;
		_logger = logger;
	}

	private async Task<IChannel> GetChannelAsync()
	{
		_channel ??= await _connection.GetChannelAsync();
		return _channel;
	}

	public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
	{
		var channel = await GetChannelAsync();

		var body = JsonSerializer.SerializeToUtf8Bytes(message);

		var properties = new BasicProperties
		{
			Persistent = true,  // Сохранять на диск для надежности
			ContentType = "application/json",
			Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
		};

		// Используем настройки из конфигурации
		await channel.BasicPublishAsync(
			exchange: _settings.PdfExchange,
			routingKey: _settings.PdfRoutingKey,
			mandatory: true,
			basicProperties: properties,
			body: body,
			cancellationToken: cancellationToken);

		_logger.LogDebug("Published message of type {MessageType} to exchange {Exchange} with routing key {RoutingKey}",
			typeof(T).Name, _settings.PdfExchange, _settings.PdfRoutingKey);
	}

	public async Task PublishToQueueAsync<T>(T message, string queueName, CancellationToken cancellationToken = default) where T : class
	{
		var channel = await GetChannelAsync();

		// Убеждаемся, что очередь существует
		await channel.QueueDeclareAsync(
			queue: queueName,
			durable: true,
			exclusive: false,
			autoDelete: false,
			cancellationToken: cancellationToken);

		var body = JsonSerializer.SerializeToUtf8Bytes(message);

		var properties = new BasicProperties
		{
			Persistent = true,
			ContentType = "application/json",
			Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
		};

		// Публикуем напрямую в очередь (через default exchange)
		await channel.BasicPublishAsync(
			exchange: "",
			routingKey: queueName,
			mandatory: true,
			basicProperties: properties,
			body: body,
			cancellationToken: cancellationToken);

		_logger.LogDebug("Published message to queue {QueueName}", queueName);
	}
}
