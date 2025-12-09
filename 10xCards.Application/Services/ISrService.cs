using _10xCards.Application.Common;
using _10xCards.Domain.Entities;

namespace _10xCards.Application.Services;

public interface ISrService
{
    Task<Result> RecordReviewAsync(
        Guid cardId,
        Guid userId,
        ReviewResult result,
        CancellationToken cancellationToken = default);
}
