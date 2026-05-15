using Shared.Enums;

namespace Shared.Models;

public class GetDocumentsResponse
{
	public List<DocumentInfo> Documents { get; set; } = new();
	public int TotalCount { get; set; }
}

public class DocumentInfo
{
	public Guid Id { get; set; }
	public string FileName { get; set; } = string.Empty;
	public int TotalPages { get; set; }
	public ProcessingStatus Status { get; set; }
	public DateTime UploadedAt { get; set; }
}