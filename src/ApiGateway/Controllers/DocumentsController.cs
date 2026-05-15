using Infrastructure.RabbitMq;

using Microsoft.AspNetCore.Mvc;

using Shared.Models;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
	private readonly ILogger<DocumentsController> _logger;
	private readonly IRabbitMqRpcClient _rpcClient;

	public DocumentsController(
		ILogger<DocumentsController> logger,
		IRabbitMqRpcClient rpcClient)
	{
		_logger = logger;
		_rpcClient = rpcClient;
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
			"rpc.get_documents");

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
			"rpc.get_pages");

		if(response.Pages.Count == 0)
			return NotFound($"No pages found for document {id}");

		return Ok(response.Pages);
	}
}