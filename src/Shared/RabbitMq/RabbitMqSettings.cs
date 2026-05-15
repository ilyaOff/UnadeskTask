namespace Shared.RabbitMq;

public class RabbitMqSettings
{
	public string HostName { get; set; } = "localhost";
	public int Port { get; set; } = 5672;
	public string UserName { get; set; } = "admin";
	public string Password { get; set; } = "admin123";
	public string VirtualHost { get; set; } = "/";

	// Очереди для команд
	public string PdfProcessingQueue { get; set; } = "pdf.processing.queue";

	// Очереди для RPC запросов
	public string RpcGetDocumentsQueue { get; set; } = "rpc.get_documents";
	public string RpcGetPagesQueue { get; set; } = "rpc.get_pages";

	// Обменники
	public string PdfExchange { get; set; } = "pdf.exchange";
	public string PdfRoutingKey { get; set; } = "pdf.process";
}
