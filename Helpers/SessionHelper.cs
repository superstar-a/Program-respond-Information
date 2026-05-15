using QLTTYKPH.Models;

namespace QLTTYKPH.Helpers
{
    public static class SessionHelper
    {
        private const string UserId = "UserId";
        private const string UserName = "UserName";
        private const string UserFullName = "UserFullName";
        private const string UserRole = "UserRole";

        public static void SetUser(ISession session, User user)
        {
            session.SetInt32(UserId, user.Id);
            session.SetString(UserName, user.Username);
            session.SetString(UserFullName, user.FullName);
            session.SetString(UserRole, user.Role.ToString());
        }

        public static int? GetUserId(ISession session) => session.GetInt32(UserId);
        public static string? GetUserName(ISession session) => session.GetString(UserName);
        public static string? GetUserFullName(ISession session) => session.GetString(UserFullName);
        public static string? GetUserRole(ISession session) => session.GetString(UserRole);

        public static bool IsLoggedIn(ISession session) => session.GetInt32(UserId).HasValue;

        public static bool IsAdmin(ISession session) => session.GetString(UserRole) == "Admin";
        public static bool IsStaff(ISession session) => session.GetString(UserRole) == "Staff";
        public static bool IsStudent(ISession session) => session.GetString(UserRole) == "Student";

        public static void Clear(ISession session) => session.Clear();
    }
}
