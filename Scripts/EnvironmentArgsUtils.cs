using System.Collections.Generic;

public static class EnvironmentArgsUtils
{
    public static string ReadArgs(string[] args, string argName, string defaultValue = null)
    {
        if (args == null)
            return defaultValue;

        var argsList = new List<string>(args);
        if (!argsList.Contains(argName))
            return defaultValue;

        var index = argsList.FindIndex(0, a => a.Equals(argName));
        return args[index + 1];
    }

    public static int ReadArgsInt(string[] args, string argName, int defaultValue = -1)
    {
        var number = ReadArgs(args, argName, defaultValue.ToString());
        var result = defaultValue;
        if (int.TryParse(number, out result))
            return result;
        return defaultValue;
    }

    public static bool IsArgsProvided(string[] args, string argName)
    {
        if (args == null)
            return false;

        var argsList = new List<string>(args);
        return argsList.Contains(argName);
    }
}
