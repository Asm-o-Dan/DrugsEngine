using Domain.Validators;

namespace Domain.Entities;

/// <summary>
/// Избранное лекарство пользователя
/// </summary>
public class FavoriteDrug : BaseEntity<FavoriteDrug>
{
    /// <summary>
    /// Конструктор для создания экземпляра FavoriteDrug
    /// </summary>
    /// <param name="profileId">Идентификатор профиля пользователя</param>
    /// <param name="externalUserId">Идентификатор пользователя во внешней системе</param>
    /// <param name="drugStoreId">Идентификатор аптеки, где хранится лекарство (может быть пустым)</param>
    /// <param name="drugId">Идентификатор лекарства</param>
    /// <param name="drug">Экземпляр лекарства</param>
    /// <param name="drugStore">Экземпляр аптеки, где хранится лекарство (может быть пустым)</param>
    /// <param name="userProfile">Экземпляр профиля пользователя (может быть пустым)</param>
    public FavoriteDrug(Guid profileId, string externalUserId, Guid? drugStoreId, Guid drugId, Drug drug, DrugStore? drugStore = null, UserProfile? userProfile = null)
    {
        ProfileId = profileId;
        ExternalUserId = externalUserId;
        DrugStoreId = drugStoreId;
        DrugId = drugId;
        Drug = drug;
        DrugStore = drugStore;
        UserProfile = userProfile;
        ValidateEntity(new FavoriteDrugValidator());
    }

    /// <summary>
    /// Конструктор с разрешением ProfileId из UserProfile
    /// </summary>
    public FavoriteDrug(UserProfile userProfile, Guid? drugStoreId, Guid drugId, Drug drug, DrugStore? drugStore = null)
        : this(userProfile?.Id ?? Guid.Empty, userProfile?.ExternalId ?? string.Empty, drugStoreId, drugId, drug, drugStore, userProfile)
    {
    }

    public FavoriteDrug(){}

    /// <summary>
    /// Идентификатор пользователя во внешней системе
    /// </summary>
    public string ExternalUserId { get; private init; }

    /// <summary>
    /// Идентификатор профиля пользователя
    /// </summary>
    public Guid ProfileId { get; private init; }

    /// <summary>
    /// Профиль пользователя
    /// </summary>
    public UserProfile? UserProfile { get; private set; }

    /// <summary>
    /// Идентификатор лекарства
    /// </summary>
    public Guid DrugId { get; private set; }
    
    /// <summary>
    /// Экземпляр лекарства
    /// </summary>
    public Drug Drug { get; private set; }
    
    /// <summary>
    /// Идентификатор аптеки, где хранится лекарство (может быть пустым)
    /// </summary>
    public Guid? DrugStoreId { get; private set; }
    
    /// <summary>
    /// Экземпляр аптеки, где хранится лекарство (может быть пустым)
    /// </summary>
    public DrugStore? DrugStore { get; private set; }
}