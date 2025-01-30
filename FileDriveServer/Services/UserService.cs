using MongoDB.Driver;
using Server.Models;
using BCrypt.Net;

namespace Server.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;

        public UserService(IMongoDatabase database)
        {
            _users = database.GetCollection<User>("Users");
        }

        public bool AddUser(string username, string password)
        {
            if (_users.Find(u => u.Username == username).Any()) return false;

            var user = new User
            {
                Username = username,
                Password = BCrypt.Net.BCrypt.HashPassword(password)
            };
            _users.InsertOne(user);
            return true;
        }

        public bool Authenticate(string username, string password)
        {
            var user = _users.Find(u => u.Username == username).FirstOrDefault();
            return user != null && BCrypt.Net.BCrypt.Verify(password, user.Password);
        }
    }
}
