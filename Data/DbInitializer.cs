using Microsoft.AspNetCore.Identity;

namespace Clinic_System.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            string[] roles = { "Admin", "Receptionist", "Doctor", "Patient" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }



            string adminEmail = "admin@clinic.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }



            string recepEmail = "reception@clinic.com";
            var recepUser = await userManager.FindByEmailAsync(recepEmail);

            if (recepUser == null)
            {
                recepUser = new IdentityUser
                {
                    UserName = recepEmail,
                    Email = recepEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(recepUser, "Recep@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(recepUser, "Receptionist");
                }
            }
        }
    }
}