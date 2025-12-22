using GreenPartySocietyAPI.Services;
using Microsoft.Extensions.Configuration;

namespace Tests;

[TestFixture]
public class JwtServiceTests
{
    private IJwtService _jwtService = null!;

    [SetUp]
    public void SetUp()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TEST_DEV_KEY_123456789012345678901234"
            })
            .Build();

        _jwtService = new JwtService(config);
    }

    [Test]
    public void Generate_ShouldReturnNonEmptyJwt()
    {
        var token = _jwtService.Generate(
            id: "user-123",
            email: "test@example.com",
            username: "Test User"
        );

        Assert.That(token, Is.Not.Null.And.Not.Empty);
        Assert.That(token, Does.Contain("."));
    }

    [Test]
    public void GetClaims_ShouldReturnClaims_ForValidToken()
    {
        var token = _jwtService.Generate(
            "user-123",
            "test@example.com",
            "Test User"
        );

        var claims = _jwtService.GetClaims(token);

        Assert.That(claims, Is.Not.Null);
        Assert.That(claims!["id"], Is.EqualTo("user-123"));
        Assert.That(claims["email"], Is.EqualTo("test@example.com"));
        Assert.That(claims["username"], Is.EqualTo("Test User"));
    }

    [Test]
    public void GetClaims_ShouldReturnNull_ForInvalidToken()
    {
        var claims = _jwtService.GetClaims("not-a-valid-jwt");

        Assert.That(claims, Is.Null);
    }

    [Test]
    public void GetClaims_ShouldReturnNull_ForEmptyToken()
    {
        Assert.That(_jwtService.GetClaims(""), Is.Null);
        Assert.That(_jwtService.GetClaims(" "), Is.Null);
    }
}
