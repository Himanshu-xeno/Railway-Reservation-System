//Contract for database operations related to Users.
using AuthService.Models;

namespace AuthService.Repositories;

public interface IUserRepository
{
    //This method may return a User object OR null.
    //A User object must exist. Null is not expected.
    Task<User?> GetByIdAsync(int userId);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ExistsByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    
    //return a collection (list) of User objects from the database..
    Task<IEnumerable<User>> GetAllAsync();
}
