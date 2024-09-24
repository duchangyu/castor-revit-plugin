namespace CastorPlugin.Services.Contracts
{
    public interface IDigService
    {
        Action CandidatePosted { get; set; }

        Task Dig(CancellationToken cancellationToken);

        Task<int> FetchCandidateCountAsync();
    }
}
