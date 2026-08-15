using Application.Interfaces.Repositories.FavoriteDrugRepositories;
using Domain.Entities;
using Infrastructure.Dal.Repositories.BaseRepositories;

namespace Infrastructure.Dal.Repositories.FavoriteDrugRepositories;

public class FavoriteDrugWriteRepository(DrugsBotDbContext dbContext) : WriteRepository<FavoriteDrug>(dbContext),IFavoriteDrugWriteRepository;