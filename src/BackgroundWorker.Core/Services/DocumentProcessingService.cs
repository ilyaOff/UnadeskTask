using BackgroundWorker.Core.Entities;
using BackgroundWorker.Core.Interfaces;

using Microsoft.Extensions.Logging;

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
	private readonly ILogger<DocumentProcessingService> _logger;

	public event Func<ProgressUpdate, Task>? ProgressUpdated;

	public DocumentProcessingService(IDocumentRepository documentRepo,
									IPdfTextExtractor pdfExtractor,
									IFileStorageService fileStorage,
									ILogger<DocumentProcessingService> logger)
	{
		_documentRepo = documentRepo;
		_pdfExtractor = pdfExtractor;
		_fileStorage = fileStorage;
		_logger = logger;
	}

	public async Task ProcessDocumentAsync(PdfProcessingMessage message, CancellationToken token = default)
	{
		var fileId = message.FileId;
		try
		{
			_logger.LogInformation("Processing file {FileId}: {FileName}", fileId, message.FileName);

			if(await _documentRepo.DocumentExistsAsync(fileId, token))
			{
				_logger.LogWarning("Document {FileId} already exists in database", fileId);
				return;
			}

			if(!await _fileStorage.FileExistsAsync(fileId, token))
			{
				_logger.LogError("File {fileId} not found in storage", fileId);
				throw new FileNotFoundException($"File {fileId} not found in storage", fileId.ToString());
			}

			var document = new Document
			{
				Id = fileId,
				FileName = message.FileName,
				UploadedAt = message.UploadedAt,
				Status = ProcessingStatus.Pending// временно 0, обновится после извлечения
			}; 

			await _documentRepo.AddDocumentAsync(document, token);
			await _documentRepo.UpdateStatusAsync(fileId, ProcessingStatus.Extracting, token);
			
			await NotifyProgress(new ProgressUpdate
			{
				FileId = fileId,
				Status = ProcessingStatus.Extracting,
				Timestamp = DateTime.UtcNow
			});


			await using var stream = await _fileStorage.GetFileStreamAsync(fileId, token);
			
			int pagesProcessed = 0;
			int totalPages = 0;

			await foreach(var page in _pdfExtractor.ExtractTextAsync(stream, token))
			{
				pagesProcessed++;
				totalPages = page.TotalPages;

				var pageEntity = new DocumentPage
				{
					Id = Guid.NewGuid(),
					DocumentId = fileId,
					PageNumber = page.PageNumber,
					Text = page.Text
				};

				await _documentRepo.AddPageAsync(pageEntity, token);

				await NotifyProgress(new ProgressUpdate
				{
					FileId = fileId,
					Status = ProcessingStatus.Extracting,
					Timestamp = DateTime.UtcNow,
					PagesProcessed = pagesProcessed,
					TotalPages = totalPages
				});

				_logger.LogDebug("Extracted page {PageNumber}/{TotalPages} for {FileId}",
					page.PageNumber, totalPages, fileId);
			}

			await _documentRepo.UpdateDocumentPagesCountAsync(fileId, totalPages, token);

			await _documentRepo.UpdateStatusAsync(fileId, ProcessingStatus.Completed, token);
			await _documentRepo.SaveChangesAsync(token);
			
			await NotifyProgress(new ProgressUpdate
			{
				FileId = fileId,
				Status = ProcessingStatus.Completed,
				Timestamp = DateTime.UtcNow,
				PagesProcessed = totalPages,
				TotalPages = totalPages
			});

			_logger.LogInformation("Completed processing {FileId}. Pages: {TotalPages}", fileId, totalPages);
		}
		catch(FileNotFoundException)
		{
			// Специфичная обработка отсутствия файла
			await _documentRepo.UpdateStatusAsync(fileId, ProcessingStatus.Failed, token);
			await NotifyProgress(new ProgressUpdate
			{
				FileId = fileId,
				Status = ProcessingStatus.Failed,
				Timestamp = DateTime.UtcNow,
				Message = "Source file not found in storage"
			});
			throw;
		}
		catch(Exception ex)
		{
			_logger.LogError(ex, "Error processing {FileId}", fileId);
			await _documentRepo.UpdateStatusAsync(fileId, ProcessingStatus.Failed, token);

			await NotifyProgress(new ProgressUpdate
			{
				FileId = fileId,
				Status = ProcessingStatus.Failed,
				Timestamp = DateTime.UtcNow,
				Message = ex.Message
			});

			throw;
		}
	}

	private async Task NotifyProgress(ProgressUpdate progress)
	{
		if(ProgressUpdated != null)
		{
			await ProgressUpdated.Invoke(progress);
		}
	}
}