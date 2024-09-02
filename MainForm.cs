using System;
using System.Windows.Forms;
using System.IO;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using CloudSyncV2.Services;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Drawing;
using System.Net;
using System.Text;
using System.Threading;
using Google.Apis.Upload;
using System.Windows.Forms;
using System.Security.Principal;
using ZXing;
using ZXing.Windows.Compatibility;
using ZXing.Common;
using ZXing.Rendering;

namespace CloudSync
{
    public partial class MainForm : Form
    {
        private DriveService driveService;
        private const string ApplicationName = "CloudSync";
        private static readonly string[] Scopes = { DriveService.Scope.DriveFile };
        private readonly string userEmail;
        private readonly IAuthenticationService _authService;

        public MainForm(string email, IAuthenticationService authService)
        {
            InitializeComponent();
            _authService = authService;
            userEmail = email;
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                UserCredential credential = await GetCredentialAsync();
                driveService = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });
                welcomeLabel.Text = $"Welcome {userEmail}";
                await RefreshFileList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Authentication failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task<UserCredential> GetCredentialAsync()
        {
            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    userEmail,
                    System.Threading.CancellationToken.None,
                    new FileDataStore(credPath, true));
            }
        }

        private async void LogoutButton_Click(object sender, EventArgs e)
        {
            try
            {
        
                // Close the current form and show the landing page form
                this.Hide(); // Hide the current form
                if (_authService != null)
                {
                    var landingPageForm = new LandingPage(_authService); // Pass _authService to the new form
                    landingPageForm.Closed += (s, args) => this.Close(); // Close the current form when the landing page is closed
                    landingPageForm.Show();
                }
                else
                {
                    MessageBox.Show("Authentication service is not available.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to logout: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task RefreshFileList()
        {
            try
            {
                fileListView.Items.Clear();
                var request = driveService.Files.List();
                request.PageSize = 50;
                request.Fields = "nextPageToken, files(id, name, size, modifiedTime)";

                var result = await request.ExecuteAsync();
                var files = result.Files;
                if (files != null && files.Count > 0)
                {
                    foreach (var file in files)
                    {
                        var item = new ListViewItem(new[] { file.Name, FormatSize(file.Size), file.ModifiedTime?.ToString() ?? "" });
                        item.Tag = file.Id;
                        fileListView.Items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to retrieve files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string FormatSize(long? size)
        {
            if (!size.HasValue) return "N/A";
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double len = size.Value;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private async void uploadButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                        {
                            Name = Path.GetFileName(openFileDialog.FileName)
                        };

                        FilesResource.CreateMediaUpload request;
                        using (var stream = new FileStream(openFileDialog.FileName, FileMode.Open))
                        {
                            request = driveService.Files.Create(fileMetadata, stream, "");
                            request.Fields = "id";
                            await request.UploadAsync();
                        }

                        MessageBox.Show("File uploaded successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await RefreshFileList();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to upload file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void deleteButton_Click(object sender, EventArgs e)
        {
            if (fileListView.SelectedItems.Count > 0)
            {
                var fileId = (string)fileListView.SelectedItems[0].Tag;
                var fileName = fileListView.SelectedItems[0].Text;

                if (MessageBox.Show($"Are you sure you want to delete '{fileName}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        await driveService.Files.Delete(fileId).ExecuteAsync();
                        MessageBox.Show("File deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await RefreshFileList();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to delete file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a file to delete.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void refreshButton_Click(object sender, EventArgs e)
        {
            await RefreshFileList();
        }

        private async void openFileButton_Click(object sender, EventArgs e)
        {
            if (fileListView.SelectedItems.Count > 0)
            {
                var fileId = (string)fileListView.SelectedItems[0].Tag;
                var fileName = fileListView.SelectedItems[0].Text;

                try
                {
                    var request = driveService.Files.Get(fileId);
                    request.Fields = "id, name, mimeType";
                    var file = await request.ExecuteAsync();

                    var mimeType = file.MimeType;
                    var fileExtension = GetFileExtension(mimeType);

                    var tempFilePath = Path.Combine(Path.GetTempPath(), fileName + fileExtension);
                    using (var stream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        var downloadRequest = driveService.Files.Get(fileId);
                        downloadRequest.Download(stream);
                        await downloadRequest.ExecuteAsync();
                    }

                    // Check if the file exists
                    if (File.Exists(tempFilePath))
                    {
                        try
                        {
                            // Use ProcessStartInfo to avoid issues with OS compatibility
                            var processStartInfo = new ProcessStartInfo(tempFilePath)
                            {
                                UseShellExecute = true // Ensures that the file is opened with the associated application
                            };
                            Process.Start(processStartInfo);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to open file with the associated application: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("The file could not be downloaded properly.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a file to open.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private string GetFileExtension(string mimeType)
        {
            switch (mimeType)
            {
                case "application/pdf":
                    return ".pdf";
                case "application/msword":
                    return ".doc";
                case "application/vnd.openxmlformats-officedocument.wordprocessingml.document":
                    return ".docx";
                case "application/vnd.ms-excel":
                    return ".xls";
                case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet":
                    return ".xlsx";
                case "application/vnd.ms-powerpoint":
                    return ".ppt";
                case "application/vnd.openxmlformats-officedocument.presentationml.presentation":
                    return ".pptx";
                case "image/jpeg":
                    return ".jpg";
                case "image/png":
                    return ".png";
                default:
                    return ".tmp";
            }
        }

        private async void WirelessTransferButton_Click(object sender, EventArgs e)
        {
            string url = "https://cloudsync-app-git-main-jamandalleys-projects.vercel.app/";
            
            var qrCodeForm = new QRCodeForm(url);
            qrCodeForm.ShowDialog(this);
        }
    }
}