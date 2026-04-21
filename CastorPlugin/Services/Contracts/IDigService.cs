namespace CastorPlugin.Services.Contracts
{
    public interface IDigService
    {
        Action CandidatePosted { get; set; }

        bool IsAuthenticated { get; }

        Task<string> Dig(CancellationToken cancellationToken);

        Task<int> FetchCandidateCountAsync();
    }
}
