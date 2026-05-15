using BackgroundWorker.App.Data;
using BackgroundWorker.App.Services;
using BackgroundWorker.Core.Interfaces;
using BackgroundWorker.Core.Services;

using Infrastructure.FileStorage;
using Infrastructure.RabbitMq;

using Microsoft.EntityFrameworkCore;

using Shared.Interfaces;

internal class Program
{
	private static async Task Main(string[] args)
	{
		var builder = Host.CreateApplicationBuilder(args);

		AddLogging(builder);
		AddServices(builder.Services, builder.Configuration, builder.Environment);

		var app = builder.Build();

		app.Run();
	}

	private static void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
	{
		services.AddDbContext<ApplicationDbContext>(options =>
		{
			var connectionString = configuration.GetConnectionString("DefaultConnection");
			options.UseSqlite(connectionString);
			// Для разработки полезно включить детальное логирование SQL
			if(environment.IsDevelopment())
			{
				options.EnableSensitiveDataLogging();
				options.EnableDetailedErrors();
			}
		});

		services.AddScoped<IDocumentRepository>(sp =>
			sp.GetRequiredService<ApplicationDbContext>());

		services.Configure<FileStorageSettings>(configuration.GetSection("FileStorage"));
		services.AddScoped<IFileStorageService, LocalFileStorageService>();

		services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMq"));

		services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
		services.AddScoped<IRabbitMqPublisher, RabbitMqPublisher>();

		services.AddHostedService<PdfProcessingConsumer>();
		services.AddHostedService<RpcRequestHandler>();

		services.AddScoped<IPdfTextExtractor, FakePdfTextExtractor>();
		services.AddScoped<DocumentProcessingService>();
	}

	private static void AddLogging(HostApplicationBuilder builder)
	{
		builder.Logging.ClearProviders();
		builder.Logging.AddConsole();
		builder.Logging.AddDebug();
	}
}