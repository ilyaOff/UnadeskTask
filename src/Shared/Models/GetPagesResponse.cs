namespace Shared.Models;

public class GetPagesResponse
{
	public List<PageTextDto> Pages { get; set; } = new();
}

public class PageTextDto
{
	public int PageNumber { get; set; }
	public string Text { get; set; } = string.Empty;
}