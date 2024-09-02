using System.Threading.Tasks;
using System.Data.SQLite;
using Dapper;
using System.IO;
using CloudSyncV2.Services;
using CloudSyncV2.Model;

namespace CloudSyncV2.Services;

public class SQLiteUserRepository : IUserRepository
    {
        private const string ConnectionString = "Data Source=cloudsync.db;Version=3;";

        public SQLiteUserRepository()
        {
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Email TEXT NOT NULL UNIQUE,
                        IsAuthenticated INTEGER NOT NULL
                    )");
            }
        }

        public async Task<UserModel> GetUserByEmailAsync(string email)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<UserModel>(
                    "SELECT * FROM Users WHERE Email = @Email", new { Email = email });
            }
        }

        public async Task AddUserAsync(UserModel user)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                await connection.ExecuteAsync(
                    "INSERT INTO Users (Email, IsAuthenticated) VALUES (@Email, @IsAuthenticated)",
                    user);
            }
        }

        public async Task UpdateUserAsync(UserModel user)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                await connection.ExecuteAsync(
                    "UPDATE Users SET IsAuthenticated = @IsAuthenticated WHERE Email = @Email",
                    user);
            }
        }
    }