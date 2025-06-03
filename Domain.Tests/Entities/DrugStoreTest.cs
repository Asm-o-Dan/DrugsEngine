using System.Collections.Generic;
using Domain.Entities;
using Domain.Validators.Primitives;
using Domain.ValueObjects;
using FluentValidation;
using Xunit;

namespace Domain.Tests.Entities
{
    public class DrugStoreTests
    {
        // Позитивный тест: Убедимся, что корректные данные проходят валидацию
        [Fact]
        public void DrugStore_WithValidData_ShouldPassValidation()
        {
            ExistingDrugStoreNumbers.DrugStoreNumbers["PharmaNetwork"] = new HashSet<int> { };
            var address = new Address("Sample City", "Main Street", "US", 12345);
            
            var exception = Record.Exception(() => new DrugStore(address, 1, "123-456-7890", "PharmaNetwork"));

            Assert.Null(exception); // Ожидаем, что исключение не будет выброшено
        }

        // Негативный тест: Проверим, что при неверных данных выбрасывается исключение
        [Fact]
        public void DrugStore_WithInvalidData_ShouldThrowValidationException()
        {
            //ExistingDrugStoreNumbers.DrugStoreNumbers["0"] = new HashSet<int> { };

            // Arrange
            var address = new Address("jcityj", "jhgnj", null, 12777);
            // Act & Assert
            var exception = Assert.Throws<ValidationException>(() => new DrugStore(address, 41, "", "0"));
           }
    }
}