using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.ApiFunction.Model;

namespace Nexora.Application.Features.V1.SystemManagements.ApiFunction.Query;

public sealed record GetApiFunctionsQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<ApiFunctionResponse>>;

public sealed class GetApiFunctionsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetApiFunctionsQuery, PagedResult<ApiFunctionResponse>>
{
    public async Task<PagedResult<ApiFunctionResponse>> Handle(GetApiFunctionsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;
        var functions = await unitOfWork.ApiFunctionRepository.PaginationAsync(
            page,
            pageSize,
            orderBy: query => query.OrderBy(function => function.Module).ThenBy(function => function.Name),
            asNoTracking: true,
            cancellationToken: cancellationToken);

        return new PagedResult<ApiFunctionResponse>(
            functions.Data.Select(ApiFunctionResponse.FromEntity).ToArray(),
            page,
            pageSize,
            functions.Total);
    }
}

public sealed record GetApiFunctionTreeQuery : IQuery<IReadOnlyList<ApiFunctionTreeResponse>>;

public sealed class GetApiFunctionTreeQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetApiFunctionTreeQuery, IReadOnlyList<ApiFunctionTreeResponse>>
{
    public async Task<IReadOnlyList<ApiFunctionTreeResponse>> Handle(
        GetApiFunctionTreeQuery request,
        CancellationToken cancellationToken)
    {
        var functions = await unitOfWork.ApiFunctionRepository.GetAllAsync(
            orderBy: query => query.OrderBy(function => function.Module)
                .ThenBy(function => function.DisplayOrder)
                .ThenBy(function => function.Name),
            asNoTracking: true,
            cancellationToken: cancellationToken);

        return ApiFunctionTreeBuilder.Build(functions);
    }
}

public sealed record GetApiFunctionQuery(Guid ApiFunctionId) : IQuery<Result<ApiFunctionResponse>>;

public sealed class GetApiFunctionQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetApiFunctionQuery, Result<ApiFunctionResponse>>
{
    public async Task<Result<ApiFunctionResponse>> Handle(GetApiFunctionQuery request, CancellationToken cancellationToken)
    {
        var function = await unitOfWork.ApiFunctionRepository.GetAsync(
            item => item.Id == request.ApiFunctionId,
            asNoTracking: true,
            cancellationToken: cancellationToken);

        return function is null
            ? Result<ApiFunctionResponse>.Failure(Error.NotFound(CommonMessages.ApiFunction.NotFound))
            : Result<ApiFunctionResponse>.Success(ApiFunctionResponse.FromEntity(function));
    }
}
