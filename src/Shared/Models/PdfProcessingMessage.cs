namespace Shared.Models;

/// <summary>
/// Сообщение о новом PDF файле для обработки
/// </summary>
public class PdfProcessingMessage
{
	/// <summary>
	/// Уникальный идентификатор файла
	/// </summary>
	public Guid FileId { get; set; }

	/// <summary>
	/// Оригинальное имя файла
	/// </summary>
	public string FileName { get; set; } = string.Empty;

	/// <summary>
	/// Размер файла в байтах
	/// </summary>
	public long FileSize { get; set; }

	/// <summary>
	/// Время загрузки файла.
	/// </summary>
	/// <remarks>
	/// Удобно для отладки, если обработка упадёт сразу после создания записи в БД, или не начнётся обработка в целом.
	/// </remarks>
	public DateTime UploadedAt { get; set; }
}