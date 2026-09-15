namespace Store.Shared.Utility
{
    public class Constants
    {
        // Roles - defined in Store.Shared.Authorization.Roles; kept here for existing callers
        public const string Role_TrueAdmin = Authorization.Roles.TrueAdmin;
        public const string Role_DemoAdmin = Authorization.Roles.DemoAdmin;
        public const string Role_User = Authorization.Roles.User;

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