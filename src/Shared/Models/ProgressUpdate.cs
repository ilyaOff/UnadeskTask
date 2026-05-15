using Shared.Enums;

namespace Shared.Models;

/// <summary>
/// Обновление прогресса обработки файла
/// </summary>
public record ProgressUpdate
{
	/// <summary>
	/// Идентификатор файла
	/// </summary>
	public Guid FileId { get; set; }

	/// <summary>
	/// Текущий статус обработки
	/// </summary>
	public ProcessingStatus Status { get; set; }

	/// <summary>
	/// Время обновления
	/// </summary>
	public DateTime Timestamp { get; set; }

	/// <summary>
	/// Обработано страниц
	/// </summary>
	public int PagesProcessed { get; set; }

	/// <summary>
	/// Всего страниц в документе
	/// </summary>
	public int TotalPages { get; set; }

	/// <summary>
	/// Дополнительное сообщение (например, текст ошибки)
	/// </summary>
	public string? Message { get; set; }

	/// <summary>
	/// Прогресс в процентах (вычисляемое поле)
	/// </summary>
	public int ProgressPercent => TotalPages > 0
		? (int)(PagesProcessed * 100.0 / TotalPages)
		: 0;
}