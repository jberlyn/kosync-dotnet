namespace Kosync;

public static class Utility
{
    public static string HashPassword(string password)
    {
        var md5 = MD5.Create();

        md5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(password));

        var result = md5.Hash!;

        StringBuilder strBuilder = new StringBuilder();
        for (int i = 0; i < result.Length; i++)
        {
            strBuilder.Append(result[i].ToString("x2"));
        }

        return strBuilder.ToString();
    }
}