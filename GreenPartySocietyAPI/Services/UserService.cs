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

    Task<ServiceResult<UserProfileDto>> GetProfileAsync(string id);
    Task<ServiceResult<UserProfileDto>> GetMyProfileAsync(string jwt);
    Task<ServiceResult<UserProfileDto>> AssignRoleAsync(string requesterId, string targetId, string role);
    Task<ServiceResult<UserProfileDto>> UpdateBioAsync(string userId, string bio);
    Task<ServiceResult<UserProfileDto>> UpdateSubstackUrlAsync(string userId, string substackUrl);
    Task<ServiceResult<IReadOnlyList<UserProfileDto>>> GetAllProfilesAsync();
    Task<IReadOnlyList<User>> GetUsersWithSubstackAsync();
}

public sealed class UserProfileDto
{
    public string Id { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string Bio { get; set; } = "";
    public string SubstackUrl { get; set; } = "";
}

public class GetUserDetailsResponse
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Role { get; set; } = "";
    public string Bio { get; set; } = "";
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
        var user = await _userRepository.GetByEmailAsync(email);

        if (user is null || !_passwordHasher.Verify(user.Password, password))
            return ServiceResult<AuthResult>.Unauthorized("Invalid email or password.");

        return ServiceResult<AuthResult>.Ok(new AuthResult
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role
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
            Role = user.Role,
            Bio = user.Bio
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
            Role = user.Role,
            Bio = user.Bio
        });
    }

    public async Task<ServiceResult<UserProfileDto>> GetProfileAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return ServiceResult<UserProfileDto>.BadRequest("Invalid id");

        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
            return ServiceResult<UserProfileDto>.BadRequest("User not found");

        // Public profile — no email
        return ServiceResult<UserProfileDto>.Ok(new UserProfileDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = "",
            Role = user.Role,
            Bio = user.Bio,
            SubstackUrl = user.SubstackUrl
        });
    }

    public async Task<ServiceResult<UserProfileDto>> GetMyProfileAsync(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            return ServiceResult<UserProfileDto>.BadRequest("Missing token");

        if (jwt.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            jwt = jwt.Substring("Bearer ".Length);

        var claims = _jwtService.GetClaims(jwt);
        if (claims is null)
            return ServiceResult<UserProfileDto>.BadRequest("Invalid token");

        if (!claims.TryGetValue("email", out var email))
            return ServiceResult<UserProfileDto>.BadRequest("Token missing email claim");

        var user = await _userRepository.GetByEmailAsync(email);
        if (user is null)
            return ServiceResult<UserProfileDto>.BadRequest("User not found");

        // Own profile — include email
        return ServiceResult<UserProfileDto>.Ok(new UserProfileDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            Bio = user.Bio,
            SubstackUrl = user.SubstackUrl
        });
    }

    public async Task<ServiceResult<UserProfileDto>> AssignRoleAsync(string requesterId, string targetId, string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return ServiceResult<UserProfileDto>.BadRequest("Role is required.");

        var validRoles = new[] { UserRole.Admin, UserRole.Committee, UserRole.Member };
        if (!validRoles.Contains(role))
            return ServiceResult<UserProfileDto>.BadRequest($"Invalid role. Valid roles: {string.Join(", ", validRoles)}");

        var requester = await _userRepository.GetByIdAsync(requesterId);
        if (requester is null || requester.Role != UserRole.Admin)
            return ServiceResult<UserProfileDto>.Unauthorized("Only admins can assign roles.");

        var target = await _userRepository.GetByIdAsync(targetId);
        if (target is null)
            return ServiceResult<UserProfileDto>.BadRequest("Target user not found.");

        target.Role = role;
        var updated = await _userRepository.UpdateAsync(target);

        return ServiceResult<UserProfileDto>.Ok(new UserProfileDto
        {
            Id = updated.Id,
            FirstName = updated.FirstName,
            LastName = updated.LastName,
            Email = updated.Email,
            Role = updated.Role,
            Bio = updated.Bio,
            SubstackUrl = updated.SubstackUrl
        });
    }

    public async Task<ServiceResult<UserProfileDto>> UpdateBioAsync(string userId, string bio)
    {
        if (bio.Length > 500)
            return ServiceResult<UserProfileDto>.BadRequest("Bio must be 500 characters or fewer.");

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return ServiceResult<UserProfileDto>.BadRequest("User not found.");

        user.Bio = bio;
        var updated = await _userRepository.UpdateAsync(user);

        return ServiceResult<UserProfileDto>.Ok(new UserProfileDto
        {
            Id = updated.Id,
            FirstName = updated.FirstName,
            LastName = updated.LastName,
            Email = updated.Email,
            Role = updated.Role,
            Bio = updated.Bio,
            SubstackUrl = updated.SubstackUrl
        });
    }

    public async Task<ServiceResult<UserProfileDto>> UpdateSubstackUrlAsync(string userId, string substackUrl)
    {
        substackUrl = substackUrl.Trim();

        if (!string.IsNullOrEmpty(substackUrl) &&
            !Regex.IsMatch(substackUrl, @"^https?://[a-zA-Z0-9\-]+\.substack\.com/?$"))
            return ServiceResult<UserProfileDto>.BadRequest("Please enter a valid Substack URL (e.g. https://yourname.substack.com)");

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return ServiceResult<UserProfileDto>.BadRequest("User not found.");

        user.SubstackUrl = substackUrl;
        var updated = await _userRepository.UpdateAsync(user);

        return ServiceResult<UserProfileDto>.Ok(new UserProfileDto
        {
            Id = updated.Id,
            FirstName = updated.FirstName,
            LastName = updated.LastName,
            Email = updated.Email,
            Role = updated.Role,
            Bio = updated.Bio,
            SubstackUrl = updated.SubstackUrl
        });
    }

    public async Task<ServiceResult<IReadOnlyList<UserProfileDto>>> GetAllProfilesAsync()
    {
        var users = await _userRepository.GetAllAsync();
        var profiles = users.Select(u => new UserProfileDto
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = "",
            Role = u.Role,
            Bio = u.Bio,
            SubstackUrl = u.SubstackUrl
        }).ToList();
        return ServiceResult<IReadOnlyList<UserProfileDto>>.Ok(profiles);
    }

    public async Task<IReadOnlyList<User>> GetUsersWithSubstackAsync()
    {
        var all = await _userRepository.GetAllAsync();
        return all.Where(u => !string.IsNullOrWhiteSpace(u.SubstackUrl)).ToList();
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
    public string Role { get; set; } = "";
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
    public static ServiceResult<T> Unauthorized(string error) => new(false, default, error);
}
