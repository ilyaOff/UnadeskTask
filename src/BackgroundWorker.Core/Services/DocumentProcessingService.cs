using BackgroundWorker.Core.Entities;
using BackgroundWorker.Core.Interfaces;

using Shared.Enums;
using Shared.Interfaces;
using Shared.Models;

namespace BackgroundWorker.Core.Services;

/// <summary>
/// Основной сервис обработки документов
/// </summary>
public class DocumentProcessingService
{
	private readonly IDocumentRepository _documentRepo;
	private readonly IPdfTextExtractor _pdfExtractor;
	private readonly IFileStorageService _fileStorage;

	public DocumentProcessingService(IDocumentRepository documentRepo, IPdfTextExtractor pdfExtractor, IFileStorageService fileStorage)
	{
		_documentRepo = documentRepo;
		_pdfExtractor = pdfExtractor;
		_fileStorage = fileStorage;
	}

	public async Task ProcessDocumentAsync(PdfProcessingMessage message)
	{
		var fileId = message.FileId;

		var document = new Document
		{
			Id = fileId,
			FileName = message.FileName,
			UploadedAt = message.UploadedAt,
			Status = ProcessingStatus.Extracting
		};

		await _documentRepo.AddDocumentAsync(document);

		await using var stream = await _fileStorage.GetFileStreamAsync(fileId);

		await foreach(var page in _pdfExtractor.ExtractTextAsync(stream))
		{
			var pageEntity = new DocumentPage
			{
				Id = Guid.NewGuid(),
				DocumentId = fileId,
				PageNumber = page.PageNumber,
				Text = page.Text
			};

			await _documentRepo.AddPageAsync(pageEntity);
		}

		await _documentRepo.UpdateDocumentStatusAsync(fileId, ProcessingStatus.Completed);
		await _documentRepo.SaveChangesAsync();
	}
}