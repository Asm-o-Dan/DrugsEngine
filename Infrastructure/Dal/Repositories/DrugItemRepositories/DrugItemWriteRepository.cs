using Application.Interfaces.Repositories.DrugItemRepositories;
using Domain.Entities;
using Infrastructure.Dal.Repositories.BaseRepositories;

namespace Infrastructure.Dal.Repositories.DrugItemRepositories;

public class DrugItemWriteRepository(DrugsBotDbContext dbContext) : WriteRepository<DrugItem>(dbContext), IDrugItemWriteRepository;