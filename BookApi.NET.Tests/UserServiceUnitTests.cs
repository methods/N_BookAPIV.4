using System.Security.Claims;
using BookApi.NET.Models;
using BookApi.NET.Services;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;

namespace BookApi.NET.Tests;

public class UserServiceUnitTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly UserService _mockUserService;

    public UserServiceUnitTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUserService = new UserService(_mockUserRepository.Object);
    }

    private ClaimsPrincipal CreateTestClaimsPrincipal(string id, string email, string name)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, id),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, name)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task FindOrCreateUserAsync_WhenUserExists_ReturnsExistingUser()
    {
        // GIVEN an external user ID and a corresponding existing user in the database
        var externalId = "google-123";
        var existingUser = new User(externalId, "test@example.com", "Test User");
        
        _mockUserRepository
            .Setup(repo => repo.GetByExternalIdAsync(externalId))
            .ReturnsAsync(existingUser);

        var claimsPrincipal = CreateTestClaimsPrincipal(externalId, "test@example.com", "Test User");

        // WHEN FindOrCreateUserAsync is called
        var result = await _mockUserService.FindOrCreateUserAsync(claimsPrincipal);

        // THEN the existing user should be returned
        Assert.Equal(existingUser.Id, result.Id);
        
        // AND the CreateAsync method should NOT have been called
        _mockUserRepository.Verify(repo => repo.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task FindOrCreateUserAsync_WhenUserDoesNotExist_CreatesAndReturnsNewUser()
    {
        // GIVEN an external user ID that is not in the database
        var externalId = "google-456";
        _mockUserRepository
            .Setup(repo => repo.GetByExternalIdAsync(externalId))
            .ReturnsAsync((User?)null);

        // AND a new User object that will be returned by the CreateAsync method
        var newUser = new User(externalId, "new@example.com", "New User");
        _mockUserRepository
            .Setup(repo => repo.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync(newUser);
            
        var claimsPrincipal = CreateTestClaimsPrincipal(externalId, "new@example.com", "New User");

        // WHEN FindOrCreateUserAsync is called
        var result = await _mockUserService.FindOrCreateUserAsync(claimsPrincipal);

        // THEN the new user should be returned
        Assert.Equal(newUser.Id, result.Id);

        // AND the CreateAsync method should be called exactly once
        _mockUserRepository.Verify(repo => repo.CreateAsync(It.IsAny<User>()), Times.Once);
    }
}
