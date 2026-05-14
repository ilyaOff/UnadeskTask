using BackgroundWorker.Core.Interfaces;

using Shared.Enums;

namespace BackgroundWorker.Core.Entities;

/// <summary>
/// EF сущность документа
/// </summary>
public class Document
{
	/// <summary>
	/// Уникальный идентификатор файла (совпадает с FileId из сообщения)
	/// </summary>
	public Guid Id { get; set; }

	/// <summary>
	/// Оригинальное имя файла
	/// </summary>
	public string FileName { get; set; } = string.Empty;

	/// <summary>
	/// Общее количество страниц
	/// </summary>
	public int TotalPages { get; set; }

	/// <summary>
	/// Время загрузки файла
	/// </summary>
	public DateTime UploadedAt { get; set; }

	/// <summary>
	/// Статус обработки
	/// </summary>
	public ProcessingStatus Status { get; set; }

	/// <summary>
	/// Время последнего обновления
	/// </summary>
	public DateTime LastUpdatedAt { get; set; }

	/// <summary>
	/// Навигационное свойство: страницы документа
	/// </summary>
	public ICollection<DocumentPage> Pages { get; set; } = new List<DocumentPage>();
}