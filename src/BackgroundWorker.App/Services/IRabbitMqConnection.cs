using RabbitMQ.Client;

namespace BackgroundWorker.App.Services;

internal interface IRabbitMqConnection
{
	Task<IChannel> GetChannelAsync();
}