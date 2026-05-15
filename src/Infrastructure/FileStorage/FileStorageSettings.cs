namespace Infrastructure.FileStorage;

public class FileStorageSettings
{
	/// <summary>
	/// Базовый путь к директории для хранения файлов
	/// </summary>
	public string StoragePath { get; set; } = "Storage/Files";

	/// <summary>
	/// Использовать абсолютный путь (если true, StoragePath интерпретируется как абсолютный)
	/// </summary>
	public bool UseAbsolutePath { get; set; } = false;
}
