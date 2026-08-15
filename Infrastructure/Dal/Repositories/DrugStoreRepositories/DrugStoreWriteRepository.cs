using Application.Interfaces.Repositories.DrugStoreRepositories;
using Domain.Entities;
using Infrastructure.Dal.Repositories.BaseRepositories;

namespace Infrastructure.Dal.Repositories.DrugStoreRepositories;

public class DrugStoreWriteRepository(DrugsBotDbContext dbContext) : WriteRepository<DrugStore>(dbContext),IDrugStoreWriteRepository;