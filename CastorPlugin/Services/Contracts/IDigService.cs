using CastorPlugin.Core;

namespace CastorPlugin.Services.Contracts
{
    public interface IDigService
    {
        Action CandidatePosted { get; set; }

        bool IsAuthenticated { get; }

        // Progress events
        event Action<int, int, string> ProgressChanged; // (scanned, total, currentFamilyName) - total=0 means unknown
        event Action<int, int, int> DigCompleted; // (totalScanned, newRegistered, similarSkipped)

        Task<string> Dig(CancellationToken cancellationToken);

        // Alias for Dig - returns ExtractionResult for result details
        Task<ExtractionResult> DigAsync(CancellationToken cancellationToken);

        Task<int> FetchCandidateCountAsync();
    }
}
