using System.Text.RegularExpressions;
using GreenPartySocietyAPI.Models;
using GreenPartySocietyAPI.Repositories;

namespace GreenPartySocietyAPI.Services;

public interface IUserService
{
    Task<bool> ExistsAsync(string email);
    Task<ServiceResult<AddUserResponse>> AddUserAsync(User user);
    Task<bool> ValidateCredentialsAsync(string email, string password);
    Task<ServiceResult<AuthResult>> AuthenticateAsync(string email, string password);

    Task<ServiceResult<GetUserDetailsResponse>> GetUserDetailsAsync(string jwt);
    Task<ServiceResult<GetUserDetailsResponse>> GetUserByIdAsync(string id);
}

public class GetUserDetailsResponse
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}


public class UserService : IUserService
{
    private IUserRepository _userRepository;
    private IPasswordHasher _passwordHasher;
    private IJwtService _jwtService;

    public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtService jwtService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public Task<bool> ExistsAsync(string email)
    {
        return _userRepository.ExistsByEmail(email);
    }

    public async Task<ServiceResult<AddUserResponse>> AddUserAsync(User user)
    {
        if (await ExistsAsync(user.Email))
            return ServiceResult<AddUserResponse>.BadRequest("Email is already in use.");

        if (!ValidateEmailFormat(user.Email))
            return ServiceResult<AddUserResponse>.BadRequest("Invalid email format.");

        if (!ValidatePasswordFormat(user.Password))
            return ServiceResult<AddUserResponse>.BadRequest("Invalid password format.");

        if (string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.LastName))
            return ServiceResult<AddUserResponse>.BadRequest("First name and last name are required.");

        user.Password = _passwordHasher.Hash(user.Password);

        User newUser;
        try
        {
            newUser = await _userRepository.AddUserAsync(user);
        }
        catch (Exception ex)
        {
            return ServiceResult<AddUserResponse>.BadRequest(ex.Message);
        }

        return ServiceResult<AddUserResponse>.Ok(new AddUserResponse
        {
            Id = newUser.Id,
            Email = newUser.Email,
            FirstName = newUser.FirstName,
            LastName = newUser.LastName
        });
    }


    public async Task<ServiceResult<AuthResult>> AuthenticateAsync(string email, string password)
    {
        //if (!ValidateEmailFormat(email))
        //    return ServiceResult<AuthResult>.BadRequest("Invalid email format.");

        var user = await _userRepository.GetByEmailAsync(email);
        if (user is null)
            return ServiceResult<AuthResult>.BadRequest("User does not exist.");

        if (!_passwordHasher.Verify(user.Password, password))
            return ServiceResult<AuthResult>.BadRequest("Invalid email or password.");

        return ServiceResult<AuthResult>.Ok(new AuthResult
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName
        });

    }

    public async Task<ServiceResult<GetUserDetailsResponse>> GetUserDetailsAsync(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            return ServiceResult<GetUserDetailsResponse>.BadRequest("Missing token");

        if (jwt.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            jwt = jwt.Substring("Bearer ".Length);

        var claims = _jwtService.GetClaims(jwt);

        if (claims is null)
            return ServiceResult<GetUserDetailsResponse>.BadRequest("Invalid token");

        if (!claims.TryGetValue("email", out var email))
            return ServiceResult<GetUserDetailsResponse>.BadRequest("Token missing email claim");

        var user = await _userRepository.GetByEmailAsync(email);

        if (user is null)
            return ServiceResult<GetUserDetailsResponse>.BadRequest("User not found");

        return ServiceResult<GetUserDetailsResponse>.Ok(new GetUserDetailsResponse
        {
            Email = user.Email,
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
        });
    }

    public async Task<ServiceResult<GetUserDetailsResponse>> GetUserByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return ServiceResult<GetUserDetailsResponse>.BadRequest("Invalid id");

        var user = await _userRepository.GetByIdAsync(id);

        if (user is null)
            return ServiceResult<GetUserDetailsResponse>.BadRequest("User not found");

        return ServiceResult<GetUserDetailsResponse>.Ok(new GetUserDetailsResponse
        {
            Email = user.Email,
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
        });
    }

    public Task<bool> ValidateCredentialsAsync(string email, string password)
    {
        throw new NotImplementedException();
    }

    public bool ValidateEmailFormat(string userEmail)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            return false;

        var pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(userEmail, pattern);
    }
    public bool ValidatePasswordFormat(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;
        // - At least 8 characters
        // - At least one uppercase letter
        // - At least one lowercase letter
        // - At least one digit
        // - At least one special character
        var pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*(),.?""':{}|<>]).{8,}$";

        return Regex.IsMatch(password, pattern);
    }
}

public class AddUserResponse
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}

public sealed class AuthResult
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}

public class ServiceResult<T>
{
    public bool Success;
    public T? Data;
    public string? Error;

    private ServiceResult(bool success, T? data, string? error)
    {
        Data = data;
        Success = success;
        Error = error;
    }

    public static ServiceResult<T> Ok(T? data) => new(true, data, null);
    public static ServiceResult<T> BadRequest(string error) => new(false, default, error);
}