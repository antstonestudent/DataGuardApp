using System.Security.Cryptography;
using System.IO;
using System.Text;

namespace DataGuardApp.hashing
{
    public static class MD5Hasher
    {
        public static string ComputeHash(string filePath)
        {
            using (MD5 md5 = MD5.Create())
            using (FileStream stream = File.OpenRead(filePath))
            {
                byte[] hashBytes = md5.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}
