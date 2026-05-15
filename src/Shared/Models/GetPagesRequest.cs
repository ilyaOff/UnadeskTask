namespace Shared.Models;

public class GetPagesRequest
{
	public Guid DocumentId { get; set; }
	public int FromPage { get; set; } = 1;
	public int ToPage { get; set; } = 10;
}
