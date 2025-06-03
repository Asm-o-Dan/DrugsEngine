using Domain.Validators;
using Domain.ValueObjects;

namespace Domain.Entities;

/// <summary>
/// Профиль пользователя
/// /// </summary>
public class UserProfile : BaseEntity<UserProfile>
{
    /// <summary>
    /// Конструктор для инициализации профиля пользователя
    /// </summary>
    /// <param name="externalId">Внешний уникальный идентификатор пользователя</param>
    /// <param name="favoriteDrugs">Список избранных лекарств пользователя</param>
    /// <param name="email">Электронная почта пользователя</param>
    public UserProfile(string externalId, List<FavoriteDrug> favoriteDrugs, Email? email)
    {
        ExternalId = externalId;
        FavoriteDrugs = favoriteDrugs;
        Email = email;
        ValidateEntity(new UserProfileValidator());
    }
    public UserProfile(){}

    /// <summary>
    /// Внешний уникальный идентификатор пользователя
    /// </summary>
    public string ExternalId { get; private init; }

    /// <summary>
    /// Список избранных лекарств пользователя
    /// </summary>
    public ICollection<FavoriteDrug> FavoriteDrugs { get; private set; } = new List<FavoriteDrug>();

    
    /// <summary>
    /// Электронная почта пользователя
    /// </summary>
    public Email? Email { get; set; }
}