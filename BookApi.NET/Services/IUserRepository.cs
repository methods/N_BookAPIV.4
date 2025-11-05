using BookApi.NET.Models;

namespace BookApi.NET.Services;

public interface IUserRepository
{
    Task<User?> GetByExternalIdAsync(string externalId);
    Task<User> CreateAsync(User user);
}