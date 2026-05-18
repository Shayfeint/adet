using System.Security.Cryptography;
using System.Text.Json;
using ADET_Group_12.Models;

namespace ADET_Group_12.Services;

public sealed record AuthenticatedUser(int Id, string Username, string DisplayName, string Role);

public sealed class SmartQUserStore
{
    private const int PasswordIterations = 100_000;
    private readonly object _sync = new();
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private List<StoredUser> _users = [];

    public SmartQUserStore(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _filePath = Path.Combine(dataDirectory, "smartq-users.json");

        LoadUsers();
        EnsureAdminUser();
    }

    public AuthenticatedUser? ValidateCredentials(string username, string password)
    {
        lock (_sync)
        {
            var normalizedUsername = NormalizeUsername(username);
            var user = _users.FirstOrDefault(item => item.NormalizedUsername == normalizedUsername);

            if (user is null || !VerifyPassword(password, user.PasswordHash))
            {
                return null;
            }

            return ToAuthenticatedUser(user);
        }
    }

    public AuthenticatedUser CreateCustomer(RegisterInput input)
    {
        lock (_sync)
        {
            var normalizedUsername = NormalizeUsername(input.Username);
            if (_users.Any(item => item.NormalizedUsername == normalizedUsername))
            {
                throw new InvalidOperationException("That username is already taken.");
            }

            var user = new StoredUser
            {
                Id = _users.Count == 0 ? 1 : _users.Max(item => item.Id) + 1,
                Username = input.Username.Trim(),
                NormalizedUsername = normalizedUsername,
                DisplayName = input.DisplayName.Trim(),
                PasswordHash = HashPassword(input.Password),
                Role = SmartQRoles.Customer,
                CreatedAt = DateTime.UtcNow
            };

            _users.Add(user);
            SaveUsers();

            return ToAuthenticatedUser(user);
        }
    }

    private void EnsureAdminUser()
    {
        lock (_sync)
        {
            var adminUsername = NormalizeUsername("admin");
            var existingAdmin = _users.FirstOrDefault(item => item.NormalizedUsername == adminUsername);

            if (existingAdmin is not null)
            {
                existingAdmin.Role = SmartQRoles.Admin;
                existingAdmin.DisplayName = "SmartQ Admin";
                existingAdmin.PasswordHash = HashPassword("admin123");
                SaveUsers();
                return;
            }

            _users.Add(new StoredUser
            {
                Id = _users.Count == 0 ? 1 : _users.Max(item => item.Id) + 1,
                Username = "admin",
                NormalizedUsername = adminUsername,
                DisplayName = "SmartQ Admin",
                PasswordHash = HashPassword("admin123"),
                Role = SmartQRoles.Admin,
                CreatedAt = DateTime.UtcNow
            });

            SaveUsers();
        }
    }

    private void LoadUsers()
    {
        lock (_sync)
        {
            if (!File.Exists(_filePath))
            {
                _users = [];
                return;
            }

            var json = File.ReadAllText(_filePath);
            _users = JsonSerializer.Deserialize<List<StoredUser>>(json) ?? [];
        }
    }

    private void SaveUsers()
    {
        var json = JsonSerializer.Serialize(_users, _jsonOptions);
        File.WriteAllText(_filePath, json);
    }

    private static AuthenticatedUser ToAuthenticatedUser(StoredUser user)
    {
        return new AuthenticatedUser(user.Id, user.Username, user.DisplayName, user.Role);
    }

    private static string NormalizeUsername(string username)
    {
        return username.Trim().ToUpperInvariant();
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            PasswordIterations,
            HashAlgorithmName.SHA256,
            32);

        return $"{PasswordIterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedPasswordHash)
    {
        var parts = storedPasswordHash.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[1]);
        var expectedHash = Convert.FromBase64String(parts[2]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private sealed class StoredUser
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string NormalizedUsername { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = SmartQRoles.Customer;
        public DateTime CreatedAt { get; set; }
    }
}
