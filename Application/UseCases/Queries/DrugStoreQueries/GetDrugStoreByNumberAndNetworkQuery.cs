using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.DrugStoreQueries;


public record GetDrugStoreByNumberAndNetworkQuery(int Number, string Network) : IRequest<DrugStore?>;
