namespace Shared.RabbitMq;

public class RabbitMqSettings
{
	public string HostName { get; set; } = "localhost";
	public int Port { get; set; } = 5672;
	public string UserName { get; set; } = "admin";
	public string Password { get; set; } = "admin123";
	public string VirtualHost { get; set; } = "/";
	public string QueueName { get; set; } = "pdf.processing.queue";
	public string ExchangeName { get; set; } = "pdf.exchange";
	public string RoutingKey { get; set; } = "pdf.process";
}
