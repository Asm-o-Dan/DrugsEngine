using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.DrugItemQueries;

public record GetDrugItemByDrugAndPharmacyQuery(Guid DrugId, Guid DrugStoreId) : IRequest<DrugItem?>;
