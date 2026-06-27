using Microsoft.EntityFrameworkCore;
using TaskApp.Data;
using TaskApp.Models;

namespace TaskApp.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<User>> GetAllAsync() =>
        await _db.Users.ToListAsync();

    public async Task<User?> GetByIdAsync(int id) =>
        await _db.Users.FindAsync(id);

    public async Task<User> CreateAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<User?> UpdateAsync(User user)
    {
        var existing = await _db.Users.FindAsync(user.Id);
        if (existing is null) return null;

        existing.Name = user.Name;
        existing.Email = user.Email;

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return false;

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return true;
    }
}
