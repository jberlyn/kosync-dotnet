namespace Kosync;

public static class Utility
{
    public static string HashPassword(string password)
    {
        byte[] hash = MD5.HashData(Encoding.ASCII.GetBytes(password));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}