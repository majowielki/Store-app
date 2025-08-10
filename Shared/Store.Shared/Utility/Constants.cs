namespace Store.Shared.Utility
{
    public class Constants
    {
        // Roles
        public const string Role_TrueAdmin = "true-admin";
        public const string Role_DemoAdmin = "demo-admin";
        public const string Role_User = "user";

        // Demo User Credentials
        public const string DemoUserEmail = "demo@store.com";
        public const string DemoUserPassword = "Demo123!";

        // Demo Admin Credentials
        public const string DemoAdminEmail = "demo-admin@store.com";
        public const string DemoAdminPassword = "DemoAdmin123!";

        // Admin Creation Token (should be set in appsettings.json)
        public const string AdminCreationTokenKey = "AdminCreationToken";
    }
}