namespace BackgroundWorker.Core.Interfaces;

/// <summary>
/// Извлекает текст из PDF файла
/// </summary>
public interface IPdfTextExtractor
{
	/// <summary>
	/// Извлекает текст из PDF файла потоково, возвращая страницы по мере обработки
	/// </summary>
	/// <param name="pdfStream">Поток с PDF файлом</param>
	/// <param name="cancellationToken">Токен отмены</param>
	/// <returns>Асинхронная последовательность страниц с текстом</returns>
	IAsyncEnumerable<PageText> ExtractTextAsync(
		Stream pdfStream,
		CancellationToken cancellationToken = default);
}

/// <summary>
/// Текст одной страницы PDF
/// </summary>
public record PageText(
	int PageNumber,
	string Text,
	int TotalPages
);