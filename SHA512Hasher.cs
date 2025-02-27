using System.Security.Cryptography;
using System.IO;
using System.Text;

namespace DataGuardApp.hashing
{
    public static class SHA512Hasher
    {
        public static string ComputeHash(string filePath)
        {
            using (SHA512 sha512 = SHA512.Create())
            using (FileStream stream = File.OpenRead(filePath))
            {
                byte[] hashBytes = sha512.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}
