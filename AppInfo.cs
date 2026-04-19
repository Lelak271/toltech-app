using System.Reflection;
using System.Runtime.InteropServices;

namespace Toltech.App
{
    /// <summary>
    /// Fournit des informations globales et statiques sur l'application.
    /// </summary>
    public static class AppInfo
    {
        public static string VersionApp
        {
            get
            {
                var v = Assembly.GetExecutingAssembly().GetName().Version;

                // Semantic Versioning
                return $"{v.Major}.{v.Minor}.{v.Build}";
            }
        }

        public static string ProductName =>
            Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyProductAttribute>()?
                    .Product ?? "Application";

        public static string Framework =>
                    RuntimeInformation.FrameworkDescription;

        public static bool IsDebug
        {
            #if DEBUG
                        get => true;
            #else
                            get => false;
            #endif
        }

    }
}
