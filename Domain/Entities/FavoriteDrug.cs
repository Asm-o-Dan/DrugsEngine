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
    /// <param name="externalUserId">Идентификатор пользователя во внешней системе</param>
    /// <param name="drugStoreId">Идентификатор аптеки, где хранится лекарство (может быть пустым)</param>
    /// <param name="drugId">Идентификатор лекарства</param>
    /// <param name="drug">Экземпляр лекарства</param>
    /// <param name="drugStore">Экземпляр аптеки, где хранится лекарство (может быть пустым)</param>
    public FavoriteDrug(string externalUserId, Guid? drugStoreId, Guid drugId, Drug drug, DrugStore? drugStore)
    {
        ExternalUserId = externalUserId;
        DrugStoreId = drugStoreId;
        DrugId = drugId;
        Drug = drug;
        DrugStore = drugStore;
        ValidateEntity(new FavoriteDrugValidator());
    }
    public FavoriteDrug(){}
    /// <summary>
    /// Идентификатор пользователя во внешней системе
    /// </summary>
    public string ExternalUserId { get; private init; }

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

    /// <summary>
    /// Идентификатор профиля.
    /// </summary>
    public Guid ProfileId { get; private init; }
    public UserProfile UserProfile { get; private set; }
}