namespace BackgroundWorker.Core.Entities;

/// <summary>
/// EF сущность страницы документа
/// </summary>
public class DocumentPage
{
	/// <summary>
	/// Уникальный идентификатор записи
	/// </summary>
	public Guid Id { get; set; }

	/// <summary>
	/// Внешний ключ к документу
	/// </summary>
	public Guid DocumentId { get; set; }

	/// <summary>
	/// Номер страницы (1-based)
	/// </summary>
	public int PageNumber { get; set; }

	/// <summary>
	/// Извлеченный текст
	/// </summary>
	public string Text { get; set; } = string.Empty;

	/// <summary>
	/// Навигационное свойство: документ
	/// </summary>
	public Document Document { get; set; } = null!;
}