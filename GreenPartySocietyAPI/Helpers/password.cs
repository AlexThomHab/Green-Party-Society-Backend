public interface IPasswordHasher
{
    string Hash(string plaintext);
    bool Verify(string hashed, string plaintext);
}

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string plaintext) => BCrypt.Net.BCrypt.HashPassword(plaintext);
    public bool Verify(string hashed, string plaintext) => BCrypt.Net.BCrypt.Verify(plaintext, hashed);
}