using NUnit.Framework;
using System.ComponentModel.DataAnnotations;
using RestaurantSystemAPI.Models.Entities;
using System.Collections.Generic;

namespace RestaurantSystemAPI.Tests.Models
{
    [TestFixture]
    public class UserModelTests
    {
        [Test]
        public void User_WithValidData_IsValid()
        {
            // Arrange
            var user = new User
            {
                Email = "test@example.com",
                PasswordHash = "hashed_password",
                FirstName = "Иван",
                LastName = "Иванов",
                Role = UserRole.Client,
                IsActive = true
            };

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(user);

            // Act
            var isValid = Validator.TryValidateObject(user, validationContext, validationResults, true);

            // Assert
            Assert.IsTrue(isValid);
            Assert.IsEmpty(validationResults);
        }

        [Test]
        public void User_WithInvalidEmail_IsInvalid()
        {
            // Arrange
            var user = new User
            {
                Email = "invalid-email",
                PasswordHash = "hashed_password",
                FirstName = "Иван",
                LastName = "Иванов"
            };

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(user);

            // Act
            var isValid = Validator.TryValidateObject(user, validationContext, validationResults, true);

            // Assert
            Assert.IsFalse(isValid);
            Assert.IsNotEmpty(validationResults);
        }
    }
}