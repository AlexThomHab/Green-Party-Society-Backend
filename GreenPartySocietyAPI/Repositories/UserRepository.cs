using GreenPartySocietyAPI.Data;
using GreenPartySocietyAPI.Models;
using Microsoft.EntityFrameworkCore;

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
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> ExistsByEmail(string email)
    {
        return await _db.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<User> AddUserAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        return await _db.Users
            .FirstOrDefaultAsync(u => u.Id == id);
    }
}
