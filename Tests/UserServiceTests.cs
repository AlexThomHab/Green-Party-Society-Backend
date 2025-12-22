using GreenPartySocietyAPI.Models;
using GreenPartySocietyAPI.Repositories;
using GreenPartySocietyAPI.Services;
using Moq;

namespace Tests;

[TestFixture]
public class UserServiceTests
{
    private Mock<IUserRepository> _userRepository = null!;
    private Mock<IPasswordHasher> _passwordHasher = null!;
    private Mock<IJwtService> _jwtService = null!;
    private UserService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepository = new Mock<IUserRepository>();
        _passwordHasher = new Mock<IPasswordHasher>();
        _jwtService = new Mock<IJwtService>();

        _service = new UserService(
            _userRepository.Object,
            _passwordHasher.Object,
            _jwtService.Object
        );
    }

    [Test]
    public async Task AddUserAsync_ShouldFail_WhenEmailAlreadyExists()
    {
        _userRepository
            .Setup(r => r.ExistsByEmail("test@test.com"))
            .ReturnsAsync(true);

        var user = new User("FirstNameTest", "LastNameTest", "test@test.com", "Password123!");

        var result = await _service.AddUserAsync(user);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Email is already in use."));
    }

    [Test]
    public async Task AddUserAsync_ShouldHashPassword_AndSaveUser()
    {
        _userRepository
            .Setup(r => r.ExistsByEmail(It.IsAny<string>()))
            .ReturnsAsync(false);

        _passwordHasher
            .Setup(h => h.Hash("Password123!"))
            .Returns("HASHED_PASSWORD");

        _userRepository
            .Setup(r => r.AddUserAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        var user = new User("FirstNameTest", "LastNameTest", "test@test.com", "Password123!");

        var result = await _service.AddUserAsync(user);

        Assert.That(result.Success, Is.True);
        Assert.That(user.Password, Is.EqualTo("HASHED_PASSWORD"));
    }

    [Test]
    public async Task AuthenticateAsync_ShouldFail_WhenUserDoesNotExist()
    {
        _userRepository
            .Setup(r => r.GetByEmailAsync("missing@test.com"))
            .ReturnsAsync((User?)null);

        var result = await _service.AuthenticateAsync("missing@test.com", "pass");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("User does not exist."));
    }

    [Test]
    public async Task AuthenticateAsync_ShouldFail_WhenPasswordIsInvalid()
    {
        var user = new User("FirstNameTest", "LastNameTest", "test@test.com", "HASH");

        _userRepository
            .Setup(r => r.GetByEmailAsync("test@test.com"))
            .ReturnsAsync(user);

        _passwordHasher
            .Setup(h => h.Verify("HASH", "wrong"))
            .Returns(false);

        var result = await _service.AuthenticateAsync("test@test.com", "wrong");

        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task AuthenticateAsync_ShouldReturnAuthResult_WhenValid()
    {
        var user = new User("FirstNameTest", "LastNameTest", "test@test.com", "HASH")
        {
            Id = "user-1"
        };

        _userRepository
            .Setup(r => r.GetByEmailAsync("test@test.com"))
            .ReturnsAsync(user);

        _passwordHasher
            .Setup(h => h.Verify("HASH", "Password123!"))
            .Returns(true);

        var result = await _service.AuthenticateAsync("test@test.com", "Password123!");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!.Id, Is.EqualTo("user-1"));
    }
}
