using Serilog;

namespace UnityGameAssemblyPatcher
{
    internal class Logging
    {
        private static ILogger instance;
        private static ILogger CreateLogger()
        {
            return new LoggerConfiguration()
                .WriteTo
                .File(  "UnityGamePatcher.log"
                        , rollingInterval: RollingInterval.Hour,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] ({SourceContext}) - {Message}{NewLine}")
                .CreateLogger();
        }

        internal static ILogger GetLogger<T>()
        {
            return (instance ??= CreateLogger()).ForContext<T>();
        }
    }
}
