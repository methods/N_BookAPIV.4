using System.Security.Claims;
using BookApi.NET.Models;

namespace BookApi.NET.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    
    public async Task<User> FindOrCreateUserAsync(ClaimsPrincipal claimsPrincipal)
    {
        var externalId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value;
        var fullName = claimsPrincipal.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(externalId) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(fullName))
        {
            throw new InvalidOperationException("User claims are missing required information.");
        }

        var existingUser = await _userRepository.GetByExternalIdAsync(externalId);
        if (existingUser is not null)
        {
            return existingUser;
        }

        var newUser = new User(externalId, email, fullName);
        return await _userRepository.CreateAsync(newUser);
    }
}