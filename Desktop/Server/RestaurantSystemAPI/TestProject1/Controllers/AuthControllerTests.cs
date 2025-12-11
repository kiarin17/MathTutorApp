using NUnit.Framework;
using RestaurantSystemAPI.Controllers;

namespace TestProject1.Controllers
{
    [TestFixture]
    public class AuthControllerSimpleTests
    {
        [Test]
        public void CreateLoginRequest_ValidData_Success()
        {
            // Arrange & Act
            var request = new LoginRequest
            {
                Email = "test@example.com",
                Password = "Password123"
            };

            // Assert
            Assert.IsNotNull(request);
            Assert.AreEqual("test@example.com", request.Email);
            Assert.AreEqual("Password123", request.Password);
        }

        [Test]
        public void CreateRegisterRequest_ValidData_Success()
        {
            // Arrange & Act
            var request = new RegisterRequest
            {
                Email = "test@example.com",
                Password = "Password123",
                FirstName = "Иван",
                LastName = "Иванов",
                PhoneNumber = "+79991234567"
            };

            // Assert
            Assert.IsNotNull(request);
            Assert.AreEqual("test@example.com", request.Email);
            Assert.AreEqual("Иван", request.FirstName);
            Assert.AreEqual("Иванов", request.LastName);
            Assert.AreEqual("+79991234567", request.PhoneNumber);
        }

        [Test]
        public void CreateUserDto_ValidData_Success()
        {
            // Arrange & Act
            var userDto = new UserDto
            {
                Id = 1,
                Email = "test@example.com",
                FirstName = "Иван",
                LastName = "Иванов",
                Role = "Client",
                CreatedAt = System.DateTime.Now
            };

            // Assert
            Assert.IsNotNull(userDto);
            Assert.AreEqual(1, userDto.Id);
            Assert.AreEqual("test@example.com", userDto.Email);
            Assert.AreEqual("Иван", userDto.FirstName);
            Assert.AreEqual("Client", userDto.Role);
        }

        [Test]
        public void CreateLoginResponse_ValidData_Success()
        {
            // Arrange
            var userDto = new UserDto
            {
                Id = 1,
                Email = "test@example.com",
                FirstName = "Иван",
                LastName = "Иванов"
            };

            // Act
            var response = new LoginResponse
            {
                Message = "Вход выполнен успешно",
                Token = "jwt-token-123",
                User = userDto
            };

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual("Вход выполнен успешно", response.Message);
            Assert.AreEqual("jwt-token-123", response.Token);
            Assert.AreEqual("test@example.com", response.User.Email);
        }

        [Test]
        public void CreateRegisterResponse_ValidData_Success()
        {
            // Arrange
            var userDto = new UserDto
            {
                Id = 1,
                Email = "test@example.com",
                FirstName = "Иван",
                LastName = "Иванов"
            };

            // Act
            var response = new RegisterResponse
            {
                Message = "Пользователь успешно зарегистрирован",
                Token = "jwt-token-456",
                User = userDto
            };

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual("Пользователь успешно зарегистрирован", response.Message);
            Assert.AreEqual("jwt-token-456", response.Token);
            Assert.AreEqual("test@example.com", response.User.Email);
        }
    }
}