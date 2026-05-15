namespace Shared.Models;

public class GetDocumentsRequest
{
	public int Page { get; set; } = 1;
	public int PageSize { get; set; } = 20;
}