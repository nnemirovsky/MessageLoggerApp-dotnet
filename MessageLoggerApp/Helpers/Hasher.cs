using System.Security.Cryptography;
using System.Text;

namespace MessageLoggerApp.Helpers;

public class Hasher
{
    public static string GetHash(string data, string salt)
    {
        return GetHash(data + salt);
    }

    public static string GetHash(string data)
    {
        return Convert.ToBase64String(GetHash(Encoding.ASCII.GetBytes(data)));
    }

    public static byte[] GetHash(byte[] data)
    {
        return SHA512.Create().ComputeHash(data);
    }
}
