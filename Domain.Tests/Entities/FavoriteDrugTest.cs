using System;
using System.Collections.Generic;
using Domain.Entities;
using Domain.ValueObjects;
using JetBrains.Annotations;
using Xunit;

namespace Domain.Tests.Entities;

[TestSubject(typeof(FavoriteDrug))]
public class FavoriteDrugTest
{

    [Fact]
    public void FavoriteDrug_WithValidData_ShouldPassValidation()
    {
        var address = new Address("Sample City", "Main Street", "US", 12345);
        var drugStore = new DrugStore(address, 1, "123-456-7890", "PharmaNetwork");
        var countrty = new Country("USA", "US");
        var drug = new Drug("Aspirin","China-Inc","RU", countrty);

        // Act & Assert
        var exception = Record.Exception(() => new FavoriteDrug("322432525", Guid.NewGuid(), Guid.NewGuid(), drug, drugStore));

        // Assert
        Assert.Null(exception); // Ожидаем, что исключение не будет выброшено
    }
    
}