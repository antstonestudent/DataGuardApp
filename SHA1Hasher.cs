using System.Security.Cryptography;
using System.IO;
using System.Text;

namespace DataGuardApp.hashing
{
    public static class SHA1Hasher
    {
        public static string ComputeHash(string filePath)
        {
            using (SHA1 sha1 = SHA1.Create())
            using (FileStream stream = File.OpenRead(filePath))
            {
                byte[] hashBytes = sha1.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}
