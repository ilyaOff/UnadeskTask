using RabbitMQ.Client;

namespace BackgroundWorker.App.Services;

public interface IRabbitMqConnection
{
	Task<IChannel> GetChannelAsync();
}