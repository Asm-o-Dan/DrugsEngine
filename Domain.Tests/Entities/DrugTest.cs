using Domain.Entities;
using FluentValidation;
using JetBrains.Annotations;
using Xunit;

namespace Domain.Tests.Entities;

[TestSubject(typeof(Drug))]
public class DrugTest
{

    [Fact]
    public void Drug_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var countrty = new Country("USA", "US");

        // Act & Assert
        var exception = Record.Exception(() => new Drug("Aspirin","China-Inc","RU", countrty));

        // Assert
        Assert.Null(exception); // Ожидаем, что исключение не будет выброшено
    }
    
    [Fact]
    public void Drug_WithInvalidData_ShouldThrowValidationException()
    {
        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => new Drug("Aspirin","","ZZ", null));
    }

    [Fact]
    public void Drug_UpdateDetails_WithValidData_ShouldUpdatePropertiesAndRetainId()
    {
        // Arrange
        var country1 = new Country("Германия", "DE");
        var country2 = new Country("Франция", "FR");
        var drug = new Drug("Аспирин Кардио", "Байер", "DE", country1);
        var originalId = drug.Id;

        // Act
        drug.UpdateDetails("Аспирин Экстра", "Байер Фарма АГ", "FR", country2);

        // Assert
        Assert.Equal(originalId, drug.Id);
        Assert.Equal("Аспирин Экстра", drug.Name);
        Assert.Equal("Байер Фарма АГ", drug.Manufacturer);
        Assert.Equal("FR", drug.CountryCodeId);
        Assert.Equal(country2, drug.Country);
    }

    [Fact]
    public void Drug_UpdateDetails_WithInvalidData_ShouldThrowValidationException()
    {
        // Arrange
        var country = new Country("Германия", "DE");
        var drug = new Drug("Аспирин Кардио", "Байер", "DE", country);

        // Act & Assert (empty name / manufacturer)
        Assert.Throws<ValidationException>(() => drug.UpdateDetails("", "Байер", "DE", country));
        Assert.Throws<ValidationException>(() => drug.UpdateDetails("Аспирин", "", "DE", country));
    }
}