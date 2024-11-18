using MongoDB.Driver;
using System.Collections.Generic;

namespace Services;
public class UserService
{
    private readonly IMongoCollection<User> _users;
    private static readonly object _lock = new object(); 

    private static readonly UserService _instance = new UserService();

    private UserService()
    {
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase("Drive");
        _users = database.GetCollection<User>("Users");
    }

    public static UserService Instance
    {
        get
        {
            return _instance;
        }
    }

    public User GetUser(string username)
    {
        lock (_lock)
        {
            return _users.Find(user => user.Username == username).FirstOrDefault();
        }
    }

    public void AddUser(string username, string password)
    {
        lock (_lock)
        {
            var newUser = new User { Username = username, Password = password };
            _users.InsertOne(newUser);
        }
    }

    public bool Authenticate(string username, string password)
    {
        lock (_lock)
        {
            var user = GetUser(username);
            return user != null && user.Password == password;
        }
    }

    public List<User> GetAllUsers()
    {
        lock (_lock)
        {
            return _users.Find(user => true).ToList();
        }
    }
}
