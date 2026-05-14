
using BackgroundWorker.App.Data;
using BackgroundWorker.Core.Interfaces;

using Microsoft.EntityFrameworkCore;

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
	}

	private static void AddLogging(HostApplicationBuilder builder)
	{
		builder.Logging.ClearProviders();
		builder.Logging.AddConsole();
		builder.Logging.AddDebug();
	}
}