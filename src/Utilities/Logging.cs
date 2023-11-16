using Serilog;

namespace UnityGameAssemblyPatcher
{
    internal class Logging
    {
        private static ILogger Instance;
        private static ILogger CreateLogger()
        {
            return new LoggerConfiguration()
                .WriteTo
                .File(  "UnityGamePatcher.txt"
                        , rollingInterval: RollingInterval.Hour)
                .CreateLogger();
        }

        internal static ILogger GetInstance()
        {
            return Instance ??= CreateLogger();
        }
    }
}
