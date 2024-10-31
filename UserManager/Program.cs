using Microsoft.Extensions.Configuration;
using Npgsql;

using BCrypt.Net;

class Program
{
    static IConfiguration Configuration;

    static Program()
    {
        Configuration = BuildConfiguration();
    }

    static IConfiguration BuildConfiguration()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? throw new Exception("ASPNETCORE_ENVIRONMENT environment variable is required");
        
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    static async Task Main(string[] args)
    {
        if (!IsLocalEnvironment())
        {
            Console.WriteLine("This tool can only be run in local development environments");
            Console.WriteLine($"Current Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
            return;
        }

        Console.WriteLine("Local User Management Tool");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("This tool is for development use only - DO NOT COMMIT TO SOURCE CONTROL!");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("-------------------------");
        
        try 
        {
            // Validate connection string exists
            _ = GetConnectionString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Configuration Error: {ex.Message}");
            Console.WriteLine("Please check your appsettings.json or user secrets");
            return;
        }

        Console.Write("Email: ");
        string? email = Console.ReadLine();
        
        if (string.IsNullOrEmpty(email))
        {
            Console.WriteLine("Email is required");
            return;
        }

        Console.Write("Password: ");
        string? password = Console.ReadLine();
        
        if (string.IsNullOrEmpty(password))
        {
            Console.WriteLine("Password is required");
            return;
        }

        await CreateUser(email, password);
    }

    static bool IsLocalEnvironment()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return environment?.ToLower() == "development";
    }

    static string GetConnectionString() => 
        Configuration["ConnectionString"] 
        ?? throw new Exception("Connection string not found");

    static async Task CreateUser(string email, string password)
    {
        using var conn = new NpgsqlConnection(GetConnectionString());
        await conn.OpenAsync();
        
        using var transaction = await conn.BeginTransactionAsync();
        try
        {
            // Create user
            string userId = await CreateUserRecord(conn, email);
            
            // Create password
            string salt = BCrypt.Net.BCrypt.GenerateSalt();
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password, salt);
            await CreatePasswordRecord(conn, userId, passwordHash, salt);

            await transaction.CommitAsync();
            Console.WriteLine($"User created successfully. ID: {userId}");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Error creating user: {ex.Message}");
            
            // Additional debug information in development
            if (IsLocalEnvironment())
            {
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }

    static async Task<string> CreateUserRecord(NpgsqlConnection conn, string email)
    {
        using var cmd = new NpgsqlCommand(
            "insert into users (email) values (@email) returning id",
            conn
        );
        cmd.Parameters.AddWithValue("email", email);
        var result = await cmd.ExecuteScalarAsync();
        
        if (result == null)
            throw new Exception("Failed to create user record");
            
        return result?.ToString() ?? throw new Exception("Failed to create user record");
    }

    static async Task CreatePasswordRecord(NpgsqlConnection conn, string userId, string passwordHash, string salt)
    {
        using var cmd = new NpgsqlCommand(
            "insert into user_passwords (user_id, password_hash, salt) values (@userId, @passwordHash, @salt)",
            conn
        );
        cmd.Parameters.AddWithValue("userId", Guid.Parse(userId));
        cmd.Parameters.AddWithValue("passwordHash", passwordHash);
        cmd.Parameters.AddWithValue("salt", salt);
        await cmd.ExecuteNonQueryAsync();
    }
}