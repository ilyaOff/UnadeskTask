
using BackgroundWorker.App.Data;
using BackgroundWorker.App.Services;
using BackgroundWorker.Core.Interfaces;
using BackgroundWorker.Core.Services;

using Infrastructure.FileStorage;

using Microsoft.EntityFrameworkCore;

using Shared.Interfaces;
using Shared.RabbitMq;

internal class Program
{
	private static async Task Main(string[] args)
	{


		var builder = Host.CreateApplicationBuilder(args);


		AddLogging(builder);
		AddServices(builder);

		var app = builder.Build();

		app.Run();
	}

	private static void AddServices(HostApplicationBuilder builder)
	{
		builder.Services.AddDbContext<ApplicationDbContext>(options =>
		{
			var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
			options.UseSqlite(connectionString);
			// Для разработки полезно включить детальное логирование SQL
			if(builder.Environment.IsDevelopment())
			{
				options.EnableSensitiveDataLogging();
				options.EnableDetailedErrors();
			}
		});

		builder.Services.AddScoped<IDocumentRepository>(sp =>
			sp.GetRequiredService<ApplicationDbContext>());

		builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
		builder.Services.AddScoped<IPdfTextExtractor, FakePdfTextExtractor>();
		builder.Services.AddScoped<DocumentProcessingService>();

		builder.Services.AddHostedService<PdfProcessingConsumer>();

		builder.Services.Configure<RabbitMqSettings>(
			builder.Configuration.GetSection("RabbitMq"));

		//builder.Services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
		//builder.Services.AddScoped<IRabbitMqPublisher, RabbitMqPublisher>();
	}

	private static void AddLogging(HostApplicationBuilder builder)
	{
		builder.Logging.ClearProviders();
		builder.Logging.AddConsole();
		builder.Logging.AddDebug();
	}
}