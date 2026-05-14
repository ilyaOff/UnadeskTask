namespace Shared.Interfaces;

/// <summary>
/// Сервис для работы с файловым хранилищем
/// </summary>
public interface IFileStorageService
{
	/// <summary>
	/// Сохраняет файл в хранилище
	/// </summary>
	/// <returns>Успешность сохранения</returns>
	Task<bool> SaveFileAsync(
		Guid fileId,
		Stream fileStream,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Получает поток файла из хранилища
	/// </summary>
	Task<Stream> GetFileStreamAsync(
			Guid fileId,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Удаляет файл из хранилища
	/// </summary>
	Task DeleteFileAsync(
		Guid fileId,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Проверяет существование файла
	/// </summary>
	Task<bool> FileExistsAsync(
		Guid fileId,
		CancellationToken cancellationToken = default);
}