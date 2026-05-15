using System.ComponentModel.DataAnnotations.Schema;

using BackgroundWorker.Core.Entities;
using BackgroundWorker.Core.Interfaces;

using Microsoft.EntityFrameworkCore;

using Shared.Enums;

namespace BackgroundWorker.App.Data;

public class ApplicationDbContext : DbContext, IDocumentRepository
{
	public DbSet<Document> Documents { get; set; }
	public DbSet<DocumentPage> DocumentPages { get; set; }

	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
	   : base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// Настройка Document
		modelBuilder.Entity<Document>(entity =>
		{
			entity.HasKey(e => e.Id);

			entity.Property(e => e.FileName)
				.IsRequired()
				.HasMaxLength(500);

			entity.Property(e => e.Status)
				.HasConversion<int>();

			entity.HasIndex(e => e.Status);
			entity.HasIndex(e => e.UploadedAt);

			entity.HasMany(e => e.Pages)
				.WithOne(e => e.Document)
				.HasForeignKey(e => e.DocumentId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		// Настройка DocumentPage
		modelBuilder.Entity<DocumentPage>(entity =>
		{
			entity.HasKey(e => e.Id);

			entity.Property(e => e.Text)
				.IsRequired()
				.HasColumnType("text"); // PostgreSQL text тип

			entity.HasIndex(e => new { e.DocumentId, e.PageNumber })
				.IsUnique(); // У одного документа не может быть двух страниц с одинаковым номером

			entity.HasIndex(e => e.PageNumber);
		});
	}

	public async Task AddDocumentAsync(Document document, CancellationToken ct = default)
	{
		await Documents.AddAsync(document, ct);
	}

	public async Task AddPageAsync(DocumentPage page, CancellationToken ct = default)
	{
		await DocumentPages.AddAsync(page, ct);
	}

	public Task UpdateDocumentStatusAsync(Guid documentId, ProcessingStatus status, CancellationToken ct = default)
	{
		// Оптимистичное обновление без загрузки всей сущности
		return Documents
			.Where(d => d.Id == documentId)
			.ExecuteUpdateAsync(setters => setters
				.SetProperty(d => d.Status, status)
				.SetProperty(d => d.LastUpdatedAt, DateTime.UtcNow), ct);
	}

	public Task<List<DocumentPage>> GetPagesAsync(Guid documentId, int fromPage, int toPage, CancellationToken ct = default)
	{
		return DocumentPages
			.Where(p => p.DocumentId == documentId &&
						p.PageNumber >= fromPage &&
						p.PageNumber <= toPage)
			.OrderBy(p => p.PageNumber)
			.ToListAsync(ct);
	}

	public Task<bool> DocumentExistsAsync(Guid documentId, CancellationToken ct = default)
	{
		return Documents.AnyAsync(d => d.Id == documentId, ct);
	}

	Task IDocumentRepository.SaveChangesAsync(CancellationToken ct)
	{
		return SaveChangesAsync(ct);
	}
}