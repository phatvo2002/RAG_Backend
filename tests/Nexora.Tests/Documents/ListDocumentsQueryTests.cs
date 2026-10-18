using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.Documents.Query;
using Nexora.Application.Features.V1.Documents.Validation;

namespace Nexora.Tests.Documents;

public sealed class ListDocumentsQueryTests
{
    [Fact]
    public void Validator_rejects_invalid_page_size_and_file_type()
    {
        var result = new ListDocumentsQueryValidator().Validate(
            new ListDocumentsQuery(Guid.NewGuid(), Page: 0, PageSize: 101, FileType: "xlsx"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Page");
        Assert.Contains(result.Errors, error => error.PropertyName == "PageSize");
        Assert.Contains(result.Errors, error => error.PropertyName == "FileType");
    }

    [Fact]
    public void Validator_allows_listing_across_all_folders()
    {
        var result = new ListDocumentsQueryValidator().Validate(new ListDocumentsQuery());

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Handler_passes_folder_filters_and_maps_repository_items()
    {
        var repository = new RecordingDocumentRepository();
        var folderId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var handler = new ListDocumentsQueryHandler(
            repository,
            new TestOrganizationProvider(),
            new TestUserContext(),
            new TestClock(now));

        var result = await handler.Handle(
            new ListDocumentsQuery(folderId, 2, 50, "manual", "pdf", "READY", Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal(2, result.Page);
        Assert.Equal(50, result.PageSize);
        Assert.Single(result.Items);
        Assert.Equal(repository.DocumentId, result.Items[0].DocumentId);
        Assert.Equal(folderId, repository.Filter!.FolderId);
        Assert.Equal("manual", repository.Filter.Search);
        Assert.Equal("pdf", repository.Filter.FileType);
        Assert.Equal("READY", repository.Filter.ProcessingStatus);
    }

    private sealed class RecordingDocumentRepository : IDocumentRepository
    {
        public Guid DocumentId { get; } = Guid.NewGuid();
        public DocumentListFilter? Filter { get; private set; }

        public Task<PagedResult<DocumentListRecord>> ListAsync(DocumentListFilter filter, CancellationToken cancellationToken)
        {
            Filter = filter;
            var item = new DocumentListRecord(
                DocumentId,
                filter.FolderId ?? Guid.Empty,
                "Tài liệu nội bộ",
                filter.DepartmentId,
                "Tài liệu nội bộ",
                "manual.pdf",
                "application/pdf",
                1024,
                "READY",
                "READY",
                null,
                filter.Now,
                filter.Now,
                filter.Now);
            return Task.FromResult(new PagedResult<DocumentListRecord>([item], filter.Page, filter.PageSize, 51));
        }
    }

    private sealed class TestOrganizationProvider : IDefaultOrganizationProvider
    {
        public Task<Guid?> GetOrganizationIdAsync(CancellationToken cancellationToken) => Task.FromResult<Guid?>(Guid.NewGuid());
    }

    private sealed class TestUserContext : IUserContext
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public bool IsAuthenticated => true;
    }

    private sealed class TestClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
