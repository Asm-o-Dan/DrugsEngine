using System;
using System.Collections.Generic;
using Domain.Entities;
using Domain.ValueObjects;
using FluentValidation;
using JetBrains.Annotations;
using Xunit;

namespace Domain.Tests.Entities;

[TestSubject(typeof(DrugItem))]
public class DrugItemTest
{
 
    /// <summary>
    ///  Положительный юнит тест для DrugItem
    /// </summary>
    [Fact]
    public void DrugItem_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var address = new Address("Sample City", "Main Street", "US", 12345);
        var drugStore = new DrugStore(address, 1, "123-456-7890", "PharmaNetwork");
        var countrty = new Country("USA", "US");
        var drug = new Drug("Aspirin","China-Inc","RU", countrty);

        var exception = Record.Exception(() =>
            new DrugItem( 3, (decimal)1515.45, drug, drugStore)
        );

        // Assert
        Assert.Null(exception);
    }
    
    /// <summary>
    ///  Негативный юнит тест для DrugItem
    /// </summary>
    [Fact]
    public void DrugItem_WithInvalidData_ShouldThrowValidationException()
    {
        // Act & Assert
        var address = new Address("Sample City", "Main Street", "US", 12345);
        var drugStore = new DrugStore(address, 1, "123-456-7890", "PharmaNetwork");
        var countrty = new Country("USA", "US");
        var drug = new Drug("Aspirin","China-Inc","RU", countrty);
        var exception = Assert.Throws<ValidationException>(() => new DrugItem( 2, (decimal)54.355, drug, drugStore));
        Assert.Contains("Cost должен быть положительным числом", exception.Message);
    }

    [Fact]
    public void DrugItem_WithNullDrugOrDrugStore_ShouldThrowValidationExceptionInsteadOfNullReferenceException()
    {
        // Arrange
        var country = new Country("USA", "US");
        var drug = new Drug("Aspirin", "Bayer", "US", country);

        // Act & Assert: should throw ValidationException, NOT NullReferenceException
        var ex1 = Assert.Throws<ValidationException>(() => new DrugItem(10, 150m, drug, null));
        Assert.Contains("не может быть пустым", ex1.Message);

        var ex2 = Assert.Throws<ValidationException>(() => new DrugItem(10, 150m, null, null));
        Assert.Contains("не может быть пустым", ex2.Message);
    }

    [Fact]
    public void DrugItem_WithDirectGuids_ShouldPassValidation()
    {
        // Arrange
        var drugId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        // Act
        var item = new DrugItem(drugId, storeId, 5, 200m);

        // Assert
        Assert.Equal(drugId, item.DrugId);
        Assert.Equal(storeId, item.DrugStoreId);
        Assert.Equal(5, item.Count);
        Assert.Equal(200m, item.Cost);
    }
}