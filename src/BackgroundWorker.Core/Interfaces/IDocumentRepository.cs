using BackgroundWorker.Core.Entities;

using Shared.Enums;

namespace BackgroundWorker.Core.Interfaces;

public interface IDocumentRepository
{
	Task<bool> DocumentExistsAsync(Guid documentId, CancellationToken ct = default);

	Task<Document?> GetDocumentAsync(Guid fileId, CancellationToken ct = default);

	Task AddDocumentAsync(Document document, CancellationToken ct = default);

	Task AddPageAsync(DocumentPage page, CancellationToken ct = default);

	Task UpdateStatusAsync(Guid documentId, ProcessingStatus status, CancellationToken ct = default);

	Task UpdateDocumentPagesCountAsync(Guid documentId, int totalPages, CancellationToken ct = default);
	
	Task<List<DocumentPage>> GetPagesAsync(Guid documentId, int fromPage, int toPage, CancellationToken ct = default);

	Task SaveChangesAsync(CancellationToken ct = default);
}

