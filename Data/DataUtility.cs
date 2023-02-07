using CSAddressBook.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CSAddressBook.Data
{
    // we don't have to instantiate this since it's static
    public static class DataUtility
    {
        public static string GetConnectionString(IConfiguration configuration)
        {
            string? connectionString = configuration.GetConnectionString("DefaultConnection");
            string? databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

            return string.IsNullOrEmpty(databaseUrl) ? connectionString! : BuildConnectionString(databaseUrl);

        }

        private static string BuildConnectionString(string databaseUrl)
        {
            // these can be var's as well (implicit instantiation)
            Uri databaseUri = new(databaseUrl);
            string[] userInfo = databaseUri.UserInfo.Split(':');
            NpgsqlConnectionStringBuilder builder = new()
            {
                Host = databaseUri.Host,
                Port = databaseUri.Port,
                Username = userInfo[0],
                Password = userInfo[1],
                Database = databaseUri.LocalPath.TrimStart('/'),
                SslMode = SslMode.Require,
                TrustServerCertificate = true
            };

            return builder.ToString();
        }

        public static async Task ManageDataAsync(IServiceProvider svcProvider)
        {
            // obtaining the necessary services based on the IServiceProvider parameter
            var dbContextSvc = svcProvider.GetRequiredService<ApplicationDbContext>();
            var userManagerSvc = svcProvider.GetRequiredService<UserManager<AppUser>>();

            // align the database by checking Migrations
            await dbContextSvc.Database.MigrateAsync();

            // Seed Demo User
            await SeedDemoUserAsync(userManagerSvc);

        }


        private static async Task SeedDemoUserAsync(UserManager<AppUser> userManager)
        {
            AppUser demoUser = new AppUser()
            {
                UserName = "demouser@addressbook.com",
                Email = "demouser@addressbook.com",
                FirstName = "Demo",
                LastName = "User",
                EmailConfirmed = true
            };

            try
            {
                AppUser? user = await userManager.FindByEmailAsync(demoUser.Email);
            
                if (user == null)
                {
                    await userManager.CreateAsync(demoUser, "T3st1!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("************** ERROR **************");
                Console.WriteLine("Error Seeding Demo Login User");
                Console.WriteLine(ex.Message);
                Console.WriteLine("***********************************");
                throw;
            }
        }
    }
}
