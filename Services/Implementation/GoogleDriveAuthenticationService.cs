using System;
using System.IO;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Util.Store;
using System.Data.SQLite;
using Google.Apis.Services;

namespace CloudSyncV2.Services
{
    public class GoogleAuthenticationService : IAuthenticationService
    {
        private const string ApplicationName = "CloudSync";
        private static readonly string[] Scopes = { DriveService.Scope.DriveFile };
        private const string ConnectionString = "Data Source=cloudsync.db;Version=3;";

        public async Task<bool> AuthenticateUserAsync(string email)
        {
            try
            {
                UserCredential credential = await GetCredentialAsync(email);
                var driveService = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });

                // Test the authentication by trying to access the user's files
                var request = driveService.Files.List();
                request.PageSize = 1;
                request.Fields = "files(id, name)";
                var result = await request.ExecuteAsync();

                // If we get here without an exception, authentication was successful
                await UpdateAuthenticationStatusAsync(email, true);
                return true;
            }
            catch (Exception)
            {
                // Authentication failed
                await UpdateAuthenticationStatusAsync(email, false);
                return false;
            }
        }

        private async Task<UserCredential> GetCredentialAsync(string email)
        {
            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    email,
                    CancellationToken.None,
                    new FileDataStore(credPath, true));
            }
        }

        public async Task<bool> IsUserAuthenticatedAsync(string email)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT IsAuthenticated FROM Users WHERE Email = @Email";
                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    var result = await command.ExecuteScalarAsync();
                    return result != null && Convert.ToInt32(result) == 1;
                }
            }
        }

        public async Task UpdateAuthenticationStatusAsync(string email, bool isAuthenticated)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                await connection.OpenAsync();
                string sql = "INSERT OR REPLACE INTO Users (Email, IsAuthenticated) VALUES (@Email, @IsAuthenticated)";
                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@IsAuthenticated", isAuthenticated ? 1 : 0);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}