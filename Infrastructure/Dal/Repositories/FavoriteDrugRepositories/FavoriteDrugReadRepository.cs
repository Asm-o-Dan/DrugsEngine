using Application.Interfaces.Repositories.FavoriteDrugRepositories;
using Domain.Entities;
using Infrastructure.Dal.Repositories.BaseRepositories;

namespace Infrastructure.Dal.Repositories.FavoriteDrugRepositories;

public class FavoriteDrugReadRepository(DrugsBotDbContext dbContext) : ReadRepository<FavoriteDrug>(dbContext),IFavoriteDrugReadRepository;