using System.Reflection;

namespace NotificationService.ApplicationService
{
    public class AssemblyReference
    {
        public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
    }
}
