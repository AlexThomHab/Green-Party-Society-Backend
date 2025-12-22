namespace GreenPartySocietyAPI.Models
{
    public interface IUser
    {
        string Id { get; set; }
        string Email { get; set; }
        string Password { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
    }
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";

        public string Email { get; set; } = "";
        public string Password { get; set; } = "";

        public User() { }

        public User(string firstName, string lastName, string email, string password)
        {
            FirstName = firstName.Trim();
            LastName = lastName.Trim();
            Email = email.Trim().ToLowerInvariant();
            Password = password;
        }
    }

}