using System.Security.Cryptography;
using System.Text;
namespace z
{
    public class Helper
    {
        public static string Hash(string input)
        {
            using HMACSHA256 hmac = new HMACSHA256(Encoding.ASCII.GetBytes("zmkadir"));
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = hmac.ComputeHash(inputBytes);
            var sb = new StringBuilder();
            foreach (var b in hashBytes)
            {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
