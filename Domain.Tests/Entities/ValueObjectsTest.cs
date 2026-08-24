using Domain.ValueObjects;
using Xunit;

namespace Domain.Tests.Entities;

public class ValueObjectsTest
{
    [Fact]
    public void Address_EqualityAndHashCode_ShouldWorkCorrectly()
    {
        var addr1 = new Address("Тирасполь", "Ленина", "10", 3300);
        var addr2 = new Address("Тирасполь", "Ленина", "10", 3300);
        var addr3 = new Address("Бендеры", "Суворова", "5", 3200);

        Assert.Equal(addr1, addr2);
        Assert.True(addr1 == addr2);
        Assert.False(addr1 != addr2);
        Assert.Equal(addr1.GetHashCode(), addr2.GetHashCode());

        Assert.NotEqual(addr1, addr3);
        Assert.True(addr1 != addr3);
    }

    [Fact]
    public void Email_EqualityAndHashCode_ShouldWorkCorrectly()
    {
        var email1 = new Email("test@example.com");
        var email2 = new Email("test@example.com");
        var email3 = new Email("other@example.com");

        Assert.Equal(email1, email2);
        Assert.True(email1 == email2);
        Assert.Equal(email1.GetHashCode(), email2.GetHashCode());

        Assert.NotEqual(email1, email3);
        Assert.True(email1 != email3);
    }

    [Fact]
    public void ValueObjects_WithNullComparison_ShouldNotThrow()
    {
        var email = new Email("test@example.com");
        Assert.False(email.Equals(null));
        Assert.False(email == null);
        Assert.True(email != null);
    }
}
