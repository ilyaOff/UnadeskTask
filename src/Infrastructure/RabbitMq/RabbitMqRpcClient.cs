using System.Collections.Concurrent;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Infrastructure.RabbitMq;

public interface IRabbitMqRpcClient : IAsyncDisposable
{
	Task<TResponse> CallAsync<TRequest, TResponse>(
		TRequest request,
		string queueName,
		CancellationToken cancellationToken = default);
}

public class RabbitMqRpcClient : IRabbitMqRpcClient
{
	private const int TimeOut = 30;

	private readonly IConnection _connection;
	private readonly IChannel _channel;
	private readonly string _replyQueueName;
	private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _pendingRequests;
	private readonly ILogger<RabbitMqRpcClient> _logger;

	public RabbitMqRpcClient(
		IOptions<RabbitMqSettings> settings,
		ILogger<RabbitMqRpcClient> logger)
	{
		_logger = logger;
		_pendingRequests = new ConcurrentDictionary<string, TaskCompletionSource<byte[]>>();

		var factory = new ConnectionFactory
		{
			HostName = settings.Value.HostName,
			Port = settings.Value.Port,
			UserName = settings.Value.UserName,
			Password = settings.Value.Password,
			VirtualHost = settings.Value.VirtualHost
		};

		_connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
		_channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

		// Объявляем временную очередь для ответов (эксклюзивная, автоудаление)
		_replyQueueName = _channel.QueueDeclareAsync(
			queue: "",
			durable: false,
			exclusive: true,
			autoDelete: true).GetAwaiter().GetResult().QueueName;

		// Запускаем слушатель ответов
		StartReplyListener();
	}

	private void StartReplyListener()
	{
		var consumer = new AsyncEventingBasicConsumer(_channel);
		consumer.ReceivedAsync += async (sender, args) =>
		{
			var correlationId = args.BasicProperties.CorrelationId;

			if(correlationId is not null 
				&& _pendingRequests.TryRemove(correlationId, out var tcs))
			{
				var body = args.Body.ToArray();
				tcs.SetResult(body);
				_logger.LogDebug("Received RPC response for correlationId: {CorrelationId}", correlationId);
			}

			await _channel.BasicAckAsync(args.DeliveryTag, false);
		};

		_channel.BasicConsumeAsync(
			queue: _replyQueueName,
			autoAck: false,
			consumer: consumer);
	}

	public async Task<TResponse> CallAsync<TRequest, TResponse>(
		TRequest request,
		string queueName,
		CancellationToken cancellationToken = default)
	{
		var correlationId = Guid.NewGuid().ToString();
		var tcs = new TaskCompletionSource<byte[]>();
		_pendingRequests.TryAdd(correlationId, tcs);

		try
		{
			var messageBody = JsonSerializer.SerializeToUtf8Bytes(request);

			var properties = new BasicProperties
			{
				CorrelationId = correlationId,
				ReplyTo = _replyQueueName,
				Persistent = true
			};

			await _channel.BasicPublishAsync(
				exchange: "",
				routingKey: queueName,
				mandatory: true,
				basicProperties: properties,
				body: messageBody,
				cancellationToken: cancellationToken);

			_logger.LogDebug("Sent RPC request to {QueueName} with correlationId: {CorrelationId}",
				queueName, correlationId);

			// Ждем ответ с таймаутом
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cts.CancelAfter(TimeSpan.FromSeconds(TimeOut));

			using(cts.Token.Register(() => tcs.TrySetCanceled()))
			{
				var responseBytes = await tcs.Task;
				var response = JsonSerializer.Deserialize<TResponse>(responseBytes) 
					?? throw new InvalidOperationException("Failed to deserialize RPC response");

				return response;
			}
		}
		catch(OperationCanceledException)
		{
			_pendingRequests.TryRemove(correlationId, out _);
			throw new TimeoutException($"RPC call to {queueName} timed out after 30 seconds");
		}
		catch(Exception ex)
		{
			_pendingRequests.TryRemove(correlationId, out _);
			_logger.LogError(ex, "RPC call to {QueueName} failed", queueName);
			throw;
		}
	}

	public async ValueTask DisposeAsync()
	{
		await _channel.CloseAsync();
		await _connection.CloseAsync();
	}
}