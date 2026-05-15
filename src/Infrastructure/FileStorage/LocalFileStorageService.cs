using Microsoft.Extensions.Logging;

using Shared.Interfaces;

namespace Infrastructure.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
	private readonly string _storagePath;
	private readonly ILogger<LocalFileStorageService> _logger;

	public LocalFileStorageService(ILogger<LocalFileStorageService> logger)
	{
		_storagePath = Path.Combine(Directory.GetCurrentDirectory(), "Storage", "Files");
		_logger = logger;

		if(!Directory.Exists(_storagePath))
		{
			Directory.CreateDirectory(_storagePath);
			_logger.LogInformation("Created storage directory at {Path}", _storagePath);
		}
	}

	public async Task<bool> SaveFileAsync(
		Guid fileId,
		Stream fileStream,
		CancellationToken cancellationToken = default)
	{

		try
		{
			var filePath = GetFilePath(fileId);

			await using(var fileStreamOutput = new FileStream(filePath, FileMode.Create, FileAccess.Write))
			{
				await fileStream.CopyToAsync(fileStreamOutput, cancellationToken);
			}

			_logger.LogDebug("Saved file {fileId} to {Path}", fileId, filePath);
			return true;
		}
		catch(Exception ex)
		{
			_logger.LogError(ex, "Failed to save file {fileId}", fileId);
			return false;
		}
	}

	public async Task<Stream> GetFileStreamAsync(
		Guid fileId,
		CancellationToken cancellationToken = default)
	{
		var filePath = GetFilePath(fileId);

		if(!File.Exists(filePath))
		{
			throw new FileNotFoundException($"File {fileId} not found at {filePath}");
		}

		var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
		return await Task.FromResult(stream);
	}

	public Task DeleteFileAsync(
		Guid fileId,
		CancellationToken cancellationToken = default)
	{
		var filePath = GetFilePath(fileId);

		if(File.Exists(filePath))
		{
			File.Delete(filePath);
			_logger.LogDebug("Deleted file {fileId}", fileId);
		}

		return Task.CompletedTask;
	}

	public Task<bool> FileExistsAsync(
		Guid fileId,
		CancellationToken cancellationToken = default)
	{
		var filePath = GetFilePath(fileId);
		return Task.FromResult(File.Exists(filePath));
	}

	private string GetFilePath(Guid fileId)
	{
		return Path.Combine(_storagePath, $"{fileId}.pdf");
	}
}