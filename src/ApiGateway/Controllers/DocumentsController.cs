using Infrastructure.RabbitMq;

using Microsoft.AspNetCore.Mvc;

using Shared.Models;
using Shared.RabbitMq;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
	private readonly ILogger<DocumentsController> _logger;
	private readonly IRabbitMqRpcClient _rpcClient;
	private readonly RabbitMqSettings _settings;

	public DocumentsController(
		ILogger<DocumentsController> logger,
		IRabbitMqRpcClient rpcClient,
		RabbitMqSettings settings)
	{
		_logger = logger;
		_rpcClient = rpcClient;
		_settings = settings;
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