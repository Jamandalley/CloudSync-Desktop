using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using CloudSyncV2.Services;

namespace CloudSync
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var services = new ServiceCollection();
            ConfigureServices(services);

            using (ServiceProvider serviceProvider = services.BuildServiceProvider())
            {
                var landingPage = serviceProvider.GetRequiredService<LandingPage>();
                Application.Run(landingPage);
            }
        }
        
        private static void ConfigureServices(ServiceCollection services)
        {
            services.AddScoped<IAuthenticationService, GoogleAuthenticationService>();
            services.AddTransient<LandingPage>();
        }
    }
}