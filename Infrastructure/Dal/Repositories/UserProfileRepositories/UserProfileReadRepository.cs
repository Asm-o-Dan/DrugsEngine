using Application.Interfaces.Repositories.UserProfileRepositories;
using Domain.Entities;
using Infrastructure.Dal.Repositories.BaseRepositories;

namespace Infrastructure.Dal.Repositories.UserProfileRepositories;

public class UserProfileReadRepository(DrugsBotDbContext dbContext) : ReadRepository<UserProfile>(dbContext),IUserProfileReadRepository;