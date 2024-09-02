using System;
using System.Windows.Forms;
using System.Threading.Tasks;
using CloudSyncV2.Services;
using CloudSyncV2.Model;
using CloudSync;

namespace CloudSync
{
    public partial class LandingPage : Form
    {
        private readonly IAuthenticationService _authService;

        public LandingPage(IAuthenticationService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        private async void authenticateButton_Click(object sender, EventArgs e)
        {
            string email = emailTextBox.Text.Trim();
            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Please enter an email address.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                bool isAuthenticated = await _authService.IsUserAuthenticatedAsync(email);
                if (isAuthenticated)
                {
                    OpenMainForm(email);
                }
                else
                {
                    authenticateButton.Enabled = false;
                    authenticateButton.Text = "Authenticating...";
                    isAuthenticated = await _authService.AuthenticateUserAsync(email);
                    if (isAuthenticated)
                    {
                        OpenMainForm(email);
                    }
                    else
                    {
                        MessageBox.Show("Authentication failed. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    authenticateButton.Enabled = true;
                    authenticateButton.Text = "Authenticate";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenMainForm(string email)
        {
            this.Hide();
            var mainForm = new MainForm(email, _authService);
            mainForm.FormClosed += (s, args) => this.Close();
            mainForm.Show();
        }
    }
}