using BackgroundWorker.Core.Interfaces;

public class FakePdfTextExtractor : IPdfTextExtractor
{
	public async IAsyncEnumerable<PageText> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default)
	{
		yield return new PageText(1, "Страница 1", 2);
		yield return new PageText(2, "Страница 2", 2);
		yield break;
	}
}