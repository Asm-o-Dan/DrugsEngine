using System;
using Domain.Entities;
using Domain.ValueObjects;
using FluentValidation;
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
        var country = new Country("USA", "US");
        var drug = new Drug("Aspirin", "China-Inc", "RU", country);
        var profileId = Guid.NewGuid();

        // Act & Assert
        var exception = Record.Exception(() => new FavoriteDrug(profileId, "322432525", Guid.NewGuid(), Guid.NewGuid(), drug, drugStore));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void FavoriteDrug_WithUserProfile_ShouldPassValidation()
    {
        var country = new Country("USA", "US");
        var drug = new Drug("Aspirin", "China-Inc", "RU", country);
        var userProfile = new UserProfile("tg_user_123");

        var favoriteDrug = new FavoriteDrug(userProfile, null, Guid.NewGuid(), drug);

        Assert.Equal(userProfile.Id, favoriteDrug.ProfileId);
        Assert.Equal("tg_user_123", favoriteDrug.ExternalUserId);
    }

    [Fact]
    public void FavoriteDrug_WithEmptyProfileId_ShouldThrowValidationException()
    {
        var country = new Country("USA", "US");
        var drug = new Drug("Aspirin", "China-Inc", "RU", country);

        Assert.Throws<ValidationException>(() =>
            new FavoriteDrug(Guid.Empty, "322432525", null, Guid.NewGuid(), drug));
    }
}