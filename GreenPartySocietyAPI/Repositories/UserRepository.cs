using GreenPartySocietyAPI.Models;

namespace GreenPartySocietyAPI.Repositories;

public interface IUserRepository
{
    Task<bool> ExistsByEmail(string email);
    Task<User> AddUserAsync(User user);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(string id);
}
public class UserRepository : IUserRepository
{
    //private readonly AppDbContext _db; TODO: ADD THIS BACK IN

    public UserRepository()
    {
    }

    public async Task<bool> ExistsByEmail(string email)
    {
        return true;
    }
    public async Task<User> AddUserAsync(User user)
    {
        return user;
    }
    public async Task<User?> GetByEmailAsync(string email)
    {
        return new User(){Email = "testUser"};
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        return new User() { Email = "testUser" };
    }
}