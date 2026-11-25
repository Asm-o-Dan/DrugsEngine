namespace Domain.Entities;

/// <summary>Сколько товара в аптеке — в тех словах, в каких это сообщает источник.</summary>
public enum StockLevel
{
    /// <summary>«Мало»</summary>
    Low = 1,
    /// <summary>«Достаточно»</summary>
    Medium = 2,
    /// <summary>«Много»</summary>
    High = 3,
}
