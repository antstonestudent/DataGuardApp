using System.Security.Cryptography;
using System.IO;
using System.Text;

namespace DataGuardApp.hashing
{
    public static class SHA256Hasher
    {
        public static string ComputeHash(string filePath)
        {
            using (SHA256 sha256 = SHA256.Create())
            using (FileStream stream = File.OpenRead(filePath))
            {
                byte[] hashBytes = sha256.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}
