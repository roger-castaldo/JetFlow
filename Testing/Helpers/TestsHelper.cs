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

}
