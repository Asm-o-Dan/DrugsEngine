using Application.Interfaces.Repositories.UserProfileRepositories;
using Domain.Entities;
using Infrastructure.Dal.Repositories.BaseRepositories;

namespace Infrastructure.Dal.Repositories.UserProfileRepositories;

public class UserProfileWriteRepository(DrugsBotDbContext dbContext)
    : WriteRepository<UserProfile>(dbContext), IUserProfileWriteRepository;