namespace Shared.Enums;

/// <summary>
/// Статус обработки документа
/// </summary>
public enum ProcessingStatus
{
	/// <summary>
	/// В очереди на обработку
	/// </summary>
	Pending = 0,

	/// <summary>
	/// Извлечение текста из PDF
	/// </summary>
	Extracting = 1,

	/// <summary>
	/// Сохранение в базу данных
	/// </summary>
	SavingToDb = 2,

	/// <summary>
	/// Обработка завершена успешно
	/// </summary>
	Completed = 3,

	/// <summary>
	/// Ошибка при обработке
	/// </summary>
	Failed = 4
}