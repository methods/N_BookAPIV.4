using BookApi.NET.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BookApi.NET.Services;

public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _usersCollection;

    public UserRepository(IOptions<BookstoreDbSettings> dbSettings, IMongoClient mongoClient)
    {
        var database = mongoClient.GetDatabase(dbSettings.Value.DatabaseName);
        _usersCollection = database.GetCollection<User>("users");
    }
    public async Task<User?> GetByExternalIdAsync(string externalId)
    {
        return await _usersCollection.Find(u => u.ExternalId == externalId).FirstOrDefaultAsync();
    }
    public async Task<User> CreateAsync(User user)
    {
        await _usersCollection.InsertOneAsync(user);
        var createdUser = await _usersCollection.Find(u => u.Id == user.Id).SingleAsync();

        return createdUser;
    }

}