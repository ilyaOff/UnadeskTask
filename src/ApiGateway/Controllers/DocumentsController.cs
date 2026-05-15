using ApiGateway.Filters;

using Infrastructure.RabbitMq;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Shared.Interfaces;
using Shared.Models;
namespace ApiGateway.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
	private readonly ILogger<DocumentsController> _logger;
	private readonly IRabbitMqRpcClient _rpcClient;
	private readonly IRabbitMqPublisher _publisher;
	private readonly IFileStorageService _fileStorage;
	private readonly RabbitMqSettings _settings;

	public DocumentsController(
		ILogger<DocumentsController> logger,
		IRabbitMqRpcClient rpcClient,
		IRabbitMqPublisher publisher,
		IFileStorageService fileStorage,
		IOptions<RabbitMqSettings> settings)
	{
		_logger = logger;
		_rpcClient = rpcClient;
		_publisher = publisher;
		_fileStorage = fileStorage;
		_settings = settings.Value;
	}

	[HttpPost("upload")]
	[ValidatePdf]
	public async Task<IActionResult> Upload(IFormFile file)
	{
		var fileId = Guid.NewGuid();

		await using var stream = file.OpenReadStream();
		bool successSave = await _fileStorage.SaveFileAsync(fileId, stream);

		if(!successSave)
		{
			_logger.LogError("Fail to save file {name}", file.FileName);
			return Problem();
		}

		await _publisher.PublishAsync(new PdfProcessingMessage
		{
			FileId = fileId,
			FileName = file.FileName,
			FileSize = file.Length,
			UploadedAt = DateTime.UtcNow
		});

		return Ok(new { FileId = fileId });
	}

	[HttpGet]
	public async Task<IActionResult> GetDocuments([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
	{
		var request = new GetDocumentsRequest
		{
			Page = page,
			PageSize = pageSize
		};

		var response = await _rpcClient.CallAsync<GetDocumentsRequest, GetDocumentsResponse>(
			request,
			_settings.RpcGetDocumentsQueue);

		return Ok(response);
	}

	[HttpGet("{id}/pages")]
	public async Task<IActionResult> GetPages(Guid id, [FromQuery] int from = 1, [FromQuery] int to = 10)
	{
		var request = new GetPagesRequest
		{
			DocumentId = id,
			FromPage = from,
			ToPage = to
		};

		var response = await _rpcClient.CallAsync<GetPagesRequest, GetPagesResponse>(
			request,
			_settings.RpcGetPagesQueue);

		if(response.Pages.Count == 0)
			return NotFound($"No pages found for document {id}");

		return Ok(response.Pages);
	}
}