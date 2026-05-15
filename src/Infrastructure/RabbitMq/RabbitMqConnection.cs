using Microsoft.Extensions.Options;

using RabbitMQ.Client;

using Shared.RabbitMq;

namespace Infrastructure.RabbitMq;

public interface IRabbitMqConnection
{
	Task<IChannel> GetChannelAsync();
}

public class RabbitMqConnection : IRabbitMqConnection
{
	private readonly RabbitMqSettings _settings;
	private IConnection? _connection;
	private IChannel? _channel;

	public RabbitMqConnection(IOptions<RabbitMqSettings> settings)
	{
		_settings = settings.Value;
	}

	public async Task<IChannel> GetChannelAsync()
	{
		if(_channel == null)
		{
			var factory = new ConnectionFactory
			{
				HostName = _settings.HostName,
				Port = _settings.Port,
				UserName = _settings.UserName,
				Password = _settings.Password,
				VirtualHost = _settings.VirtualHost
			};

			_connection = await factory.CreateConnectionAsync();
			_channel = await _connection.CreateChannelAsync();
		}

		return _channel;
	}
}