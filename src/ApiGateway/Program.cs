using Infrastructure.FileStorage;
using Infrastructure.RabbitMq;

using Shared.Interfaces;
using Shared.RabbitMq;

internal class Program
{
	private static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		IServiceCollection services = builder.Services;
		IConfiguration configuration = builder.Configuration;
		AddServices(services, configuration);

		var app = builder.Build();

		SetupMiddleware(app);

		app.Run();
	}

	private static void AddServices(IServiceCollection services, IConfiguration configuration)
	{
		services.AddEndpointsApiExplorer();
		services.AddSwaggerGen();

		services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMq"));
		services.AddSingleton<IRabbitMqRpcClient, RabbitMqRpcClient>();

		services.AddScoped<IFileStorageService, LocalFileStorageService>();
	}

	private static void SetupMiddleware(WebApplication app)
	{
		// Configure the HTTP request pipeline.
		if(app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI();
		}

		app.UseHttpsRedirection();
	}
}