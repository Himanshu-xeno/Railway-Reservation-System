using AuthService.Data;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repositories;

//This repo should provide all methods defined in interface
public class UserRepository : IUserRepository
{
    //DI
    private readonly AuthDbContext _context;

    //Passes the database context
    public UserRepository(AuthDbContext context)
    {
        _context = context; //storing db connection object for later-use
    }

    //Find User(obj) using UserId - if not found : null
    public async Task<User?> GetByIdAsync(int userId)
    {
        //Accessing Users table : _context.Users
        return await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
    }

    //Find User(obj) using Email - if not found : null
    public async Task<User?> GetByEmailAsync(string email)
    {
        //Accessing Users table : _context.Users
        return await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    //Does this email already exist?
    public async Task<bool> ExistsByEmailAsync(string email)
    {
        //AnySync() return T(if exist) else F
        return await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }

    //Inserting new User
    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);   //Not saved yet, only queued
        await _context.SaveChangesAsync();  //EF sends SQL(actual saves)
        return user;
    }

    //Get all users(obj), IEnumerable: collections of Users
    public async Task<IEnumerable<User>> GetAllAsync()
    {
        //Load all records in list
        return await _context.Users.ToListAsync();
    }
}
