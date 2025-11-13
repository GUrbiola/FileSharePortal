using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FileSharePortal
{
    public static class Settings
    {
        public static bool ClientValidationEnabled => System.Configuration.ConfigurationManager.AppSettings["ClientValidationEnabled"] == "true";
        public static bool UnobtrusiveJavaScriptEnabled => System.Configuration.ConfigurationManager.AppSettings["UnobtrusiveJavaScriptEnabled"] == "true";
        public static bool UseActiveDirectory => System.Configuration.ConfigurationManager.AppSettings["UseActiveDirectory"] == "true";
        public static string ADDomain => System.Configuration.ConfigurationManager.AppSettings["ADDomain"];
        public static string FileUploadPath => System.Configuration.ConfigurationManager.AppSettings["FileUploadPath"];
        public static string MaxFileSize => System.Configuration.ConfigurationManager.AppSettings["MaxFileSize"];
        public static string BaseUrl => System.Configuration.ConfigurationManager.AppSettings["BaseUrl"];
        public static bool ShowAdminSection => System.Configuration.ConfigurationManager.AppSettings["ShowAdminSection"] == "true";
        public static bool ShowAppearanceSection => System.Configuration.ConfigurationManager.AppSettings["ShowAppearanceSection"] == "true";
        public static bool ShowApplicationsSection => System.Configuration.ConfigurationManager.AppSettings["ShowApplicationsSection"] == "true";
        public static bool ShowNotificationsSection => System.Configuration.ConfigurationManager.AppSettings["ShowNotificationsSection"] == "true";
        public static int RedirectTimerMilliseconds
        {
            get
            {
                int timer;
                if (int.TryParse(System.Configuration.ConfigurationManager.AppSettings["RedirectTimer"], out timer))
                {
                    return timer;
                }
                return 100; // Default to 100 ms if not set or invalid
            }
        }
    }
}