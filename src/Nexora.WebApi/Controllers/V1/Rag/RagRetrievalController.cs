using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nexora.Application.Features.V1.Rag;
using Nexora.WebApi.Controllers;

namespace Nexora.WebApi.Controllers.V1.Rag;

[ApiController]
[Route("api/rag/retrieval")]
public sealed class RagRetrievalController(ISender sender) : BaseController
{
    [HttpPost]
    public async Task<IActionResult> Retrieve([FromBody] RetrievalRequest request, CancellationToken cancellationToken) =>
        this.ToSuccessResponse(
            await sender.Send(new RetrieveDocumentsQuery(request.Query), cancellationToken),
            "Retrieved relevant document chunks.");
}

public sealed record RetrievalRequest(string Query);
