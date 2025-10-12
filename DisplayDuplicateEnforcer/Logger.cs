namespace DisplayDuplicateEnforcer;

public static class Logger
{
    private static readonly string LogFile = $@"{Environment.GetEnvironmentVariable("USERPROFILE")}\Documents\dde.log";
    public static void Log(string s)
    {
        try
        {
            File.AppendAllText(LogFile, $"{DateTime.Now} {s}{Environment.NewLine}");
        }
        catch (Exception e)
        {
            // ignored
        }
    }
}