namespace Application.DTOs.DrugDTOs;

public class BaseDrugDto
{
    /// <summary>
    /// Название препарата
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Производитель
    /// </summary>
    public string Manufacturer { get; set; }
    
    /// <summary>
    /// Страна Производитель
    /// </summary>
    public string CountryName { get; set; }
    
    /// <summary>
    /// Код Страны Производителя
    /// </summary>
    public string CountryCode { get; set; }
}