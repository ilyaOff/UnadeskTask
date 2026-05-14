using BackgroundWorker.Core.Interfaces;

public class FakePdfTextExtractor : IPdfTextExtractor
{
	public async IAsyncEnumerable<PageText> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default)
	{
		yield break;
	}
}