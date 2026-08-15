using Application.Interfaces.Repositories.DrugRepositories;
using Domain.Entities;
using Infrastructure.Dal.Repositories.BaseRepositories;

namespace Infrastructure.Dal.Repositories.DrugRepositories;

public class DrugWriteRepository(DrugsBotDbContext dbContext) : WriteRepository<Drug>(dbContext),IDrugWriteRepository;