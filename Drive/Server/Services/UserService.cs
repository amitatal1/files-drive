using MongoDB.Driver;
using System.Collections.Generic;

public class UserService
{
    private readonly IMongoCollection<User> _users;

    public UserService()
    {
        // Connection to MongoDB
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase("myAppDatabase");
        _users = database.GetCollection<User>("users");
    }

    public User GetUser(string username)
    {
        return _users.Find(user => user.Username == username).FirstOrDefault();
    }

    public void AddUser(string username, string password)
    {
        var newUser = new User { Username = username, Password = password };
        _users.InsertOne(newUser);
    }

    public bool Authenticate(string username, string password)
    {
        var user = GetUser(username);
        return user != null && user.Password == password;
    }

    public List<User> GetAllUsers()
    {
        return _users.Find(user => true).ToList();
    }
}
