using System.Text.Json;

using BackgroundWorker.App.Data;

using Infrastructure.RabbitMq;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using Shared.Models;
using Shared.RabbitMq;

namespace BackgroundWorker.App.Services;

public class RpcRequestHandler : BackgroundService
{
	private readonly IRabbitMqConnection _connection;
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<RpcRequestHandler> _logger;
	private readonly RabbitMqSettings _settings;

	public RpcRequestHandler(
		IRabbitMqConnection connection,
		IServiceProvider serviceProvider,
		IOptions<RabbitMqSettings> settings,
		ILogger<RpcRequestHandler> logger)
	{
		_connection = connection;
		_serviceProvider = serviceProvider;
		_settings = settings.Value;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var channel = await _connection.GetChannelAsync();

		await channel.QueueDeclareAsync(
			queue: _settings.RpcGetDocumentsQueue,
			durable: true,
			exclusive: false,
			autoDelete: false,
			cancellationToken: stoppingToken);

		await channel.QueueDeclareAsync(
			queue: _settings.RpcGetPagesQueue,
			durable: true,
			exclusive: false,
			autoDelete: false,
			cancellationToken: stoppingToken);

		await StartConsumer(channel, _settings.RpcGetDocumentsQueue, HandleGetDocuments, stoppingToken);
		await StartConsumer(channel, _settings.RpcGetPagesQueue, HandleGetPages, stoppingToken);

		await Task.Delay(Timeout.Infinite, stoppingToken);
	}

	private async Task StartConsumer(
		IChannel channel,
		string queueName,
		Func<byte[], Task<byte[]>> handler,
		CancellationToken stoppingToken)
	{
		var consumer = new AsyncEventingBasicConsumer(channel);
		consumer.ReceivedAsync += async (sender, args) =>
		{
			try
			{
				var responseBody = await handler(args.Body.ToArray());

				var replyProps = new BasicProperties
				{
					CorrelationId = args.BasicProperties.CorrelationId
				};

				await channel.BasicPublishAsync(
					exchange: "",
					routingKey: args.BasicProperties.ReplyTo!,
					mandatory: true,
					basicProperties: replyProps,
					body: responseBody,
					cancellationToken: stoppingToken);

				await channel.BasicAckAsync(args.DeliveryTag, false);

				_logger.LogDebug("Processed RPC request from {QueueName}", queueName);
			}
			catch(Exception ex)
			{
				_logger.LogError(ex, "Error processing RPC request from {QueueName}", queueName);
				await channel.BasicNackAsync(args.DeliveryTag, false, false);
			}
		};

		await channel.BasicConsumeAsync(
			queue: queueName,
			autoAck: false,
			consumer: consumer,
			cancellationToken: stoppingToken);
	}

	private async Task<byte[]> HandleGetDocuments(byte[] requestBody)
	{
		using var scope = _serviceProvider.CreateScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		var request = JsonSerializer.Deserialize<GetDocumentsRequest>(requestBody);
		if(request == null)
			throw new InvalidOperationException("Invalid request");

		var query = dbContext.Documents.AsNoTracking();
		var totalCount = await query.CountAsync();

		var documents = await query
			.OrderByDescending(d => d.UploadedAt)
			.Skip((request.Page - 1) * request.PageSize)
			.Take(request.PageSize)
			.Select(d => new DocumentInfo
			{
				Id = d.Id,
				FileName = d.FileName,
				TotalPages = d.TotalPages,
				Status = d.Status,
				UploadedAt = d.UploadedAt
			})
			.ToListAsync();

		var response = new GetDocumentsResponse
		{
			Documents = documents,
			TotalCount = totalCount
		};

		return JsonSerializer.SerializeToUtf8Bytes(response);
	}

	private async Task<byte[]> HandleGetPages(byte[] requestBody)
	{
		using var scope = _serviceProvider.CreateScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		var request = JsonSerializer.Deserialize<GetPagesRequest>(requestBody);
		if(request == null)
			throw new InvalidOperationException("Invalid request");

		var pages = await dbContext.DocumentPages
			.Where(p => p.DocumentId == request.DocumentId &&
						p.PageNumber >= request.FromPage &&
						p.PageNumber <= request.ToPage)
			.OrderBy(p => p.PageNumber)
			.Select(p => new PageTextDto
			{
				PageNumber = p.PageNumber,
				Text = p.Text
			})
			.ToListAsync();

		var response = new GetPagesResponse
		{
			Pages = pages
		};

		return JsonSerializer.SerializeToUtf8Bytes(response);
	}
}