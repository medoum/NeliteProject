using System.Security.Cryptography;
namespace Backend.Helpers
{
    // The password hash class
	public class PasswordHasher {

        // Implements a cryptographic Random Number Generator (RNG) using the implementation provided by the cryptographic service provider (CSP).

        private static RNGCryptoServiceProvider rngCsp = new();
		private static readonly int SaltSize = 16;
        private static readonly int HashSize = 20;
        private static readonly int Iterations = 10000;

        // Hash the Password
        public static string HashPassword(string password)
        {
            byte[] salt;
            rngCsp.GetBytes(salt = new byte[SaltSize]);
            var key = new Rfc2898DeriveBytes(password, salt, Iterations);
            var hash = key.GetBytes(HashSize);

            var hashBytes = new byte[SaltSize + HashSize];
            Array.Copy(salt, 0, hashBytes, 0, SaltSize);
            Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

            var base64Hash = Convert.ToBase64String(hashBytes);
            return base64Hash;
        }

        // Passwrord verification
        public static bool VerifyPassword(string password, string base64Hash)
        {
            //var hashPassword = HashPassword(password);
            //if (hashPassword == base64Hash)
            //    return true;
            //return false;
            //var hashBytes = Convert.FromBase64String(base64Hash);

            //var salt = new byte[SaltSize];
            //Array.Copy(hashBytes, 0, salt, 0, SaltSize);

            //var key = new Rfc2898DeriveBytes(password, salt, Iterations);
            //byte[] hash = key.GetBytes(HashSize);

            //for (var i = 0; i < HashSize; i++)
            //{
            //    if (hashBytes[i + SaltSize] != hash[1])
            //        return false;

            //}
            //return true;

            var passwordAndHash = password.Split(':');
            if (passwordAndHash == null || passwordAndHash.Length != 2)
                return false;
            var salt = Convert.FromBase64String(passwordAndHash[1]);
            if (salt == null)
                return false;
            // hash the given password
            var hashOfpasswordToCheck = HashPassword(base64Hash);
            // compare both hashes
            if (string.Compare(passwordAndHash[0], hashOfpasswordToCheck) == 0)
            {
                return true;
            }
            return false;
        }
    }
}

