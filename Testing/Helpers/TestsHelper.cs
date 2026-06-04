using System.Security.Cryptography;

namespace JetFlow.Testing.Helpers;

internal static class TestsHelper
{
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string GenerateRandomString(int length)
        => new(
            Enumerable.Range(0, length)
            .Select(i => chars[RandomNumberGenerator.GetInt32(0, chars.Length)])
            .ToArray()
        );

    internal static bool SubjectMatches(object? value, string subject)
    {
        if (value==null)
            return false;
        var svalue = (value.ToString()??string.Empty).Split('.');
        var ssubject = subject.Split('.');
        if (svalue.Length != ssubject.Length)
            return false;
        for(var x=0; x<svalue.Length; x++)
        {
            if (ssubject[x] == "*")
                continue;
            if (!Equals(svalue[x],ssubject[x]))
                return false;
        }
        return true;
    }
}
