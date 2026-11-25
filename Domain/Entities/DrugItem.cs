using Domain.DomainEvents;
using Domain.Validators;
using FluentValidation;
using FluentValidation.Results;

namespace Domain.Entities;

/// <summary>
/// Промежуточная сущность для связи Drug и DrugStore
/// </summary>
public class DrugItem : BaseEntity<DrugItem>
{
    /// <summary>
    /// Конструктор сущности DrugItem
    /// </summary>`
    /// <param name="drugId">Внешний ключ для Drug</param>
    /// <param name="drugStoreId">Внешний ключ для DrugStore</param>
    /// <param name="count">Количество лекарства в магазине</param>
    /// <param name="cost">Стоимость лекарства в магазине</param>
    /// <param name="drug">Объект типа Drug, связанный с DrugItem</param>
    /// <param name="drugStore">Объект типа DrugStore, связанный с DrugItem</param>
    /// <exception cref="ValidationException">Выбрасывается, если валидация не пройдена</exception>
    /// <param name="priceUpdatedAt">Когда цену обновил источник (UTC); null — неизвестно, берётся текущее время</param>
    public DrugItem(double count, decimal cost, Drug? drug, DrugStore? drugStore, DateTime? priceUpdatedAt = null,
        StockLevel? stockLevel = null)
    {
        StockLevel = stockLevel;
        Count = count;
        Cost = cost;
        Drug = drug;
        DrugStore = drugStore;
        DrugId = drug?.Id ?? throw new ArgumentNullException(nameof(drug));
        DrugStoreId = drugStore?.Id ?? throw new ArgumentNullException(nameof(drugStore));
        SeenAt = DateTime.UtcNow;
        UpdatedAt = priceUpdatedAt ?? SeenAt;
        ValidateEntity(new DrugItemValidator());
    }
    public DrugItem(){}

    /// <summary>
    /// Внешний ключ для Drug
    /// </summary>
    public Guid DrugId { get; set; }

    /// <summary>
    /// Внешний ключ для DrugStore
    /// </summary>
    public Guid DrugStoreId { get; set; }

    /// <summary>
    /// Количество лекарства в магазине
    /// </summary>
    public double Count { get; set; }

    /// <summary>
    /// Стоимость лекарства в магазине 
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// Когда цену обновил источник (UTC) — показывается пользователю как «цена обновлена»
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Уровень остатка, если источник его сообщает (у сети «Доктор» — нет)
    /// </summary>
    public StockLevel? StockLevel { get; private set; }

    /// <summary>
    /// Когда парсер последний раз видел это предложение (UTC). Давно не виденные предложения не показываются.
    /// </summary>
    public DateTime SeenAt { get; private set; }

    /// <summary>
    /// Навигационное свойство для связи Drug и DrugItem
    /// </summary>
    public Drug? Drug { get; set; }

    /// <summary>
    /// Навигационное свойство для связи DrugStore и DrugItem
    /// </summary>
    public DrugStore? DrugStore { get; set; }
 

    public void UpdateDrugCountAndCost(double count, decimal cost, DateTime? priceUpdatedAt = null,
        StockLevel? stockLevel = null)
    {
        StockLevel = stockLevel;
        var changed = Count != count || Cost != cost;
        Count = count;
        Cost = cost;
        SeenAt = DateTime.UtcNow;
        if (priceUpdatedAt is not null) UpdatedAt = priceUpdatedAt.Value;
        else if (changed) UpdatedAt = SeenAt;
        ValidateEntity(new DrugItemValidator());
        if (changed)
            AddDomainEvent(new DrugItemUpdatedEvent());
    }
}
