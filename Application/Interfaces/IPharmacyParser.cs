using Domain.Entities;

namespace Application.Interfaces;

public interface IPharmacyParser
{
    Task<List<DrugItem>?> ParseAsync(string url,CancellationToken cancellationToken);
    Task<List<string>> ParseDrugsLinksAsync();
}