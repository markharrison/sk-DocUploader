using System.Security.Cryptography;
using System.Text;

namespace DocUploader
{
    public static class Utils
    {
        public static string GenerateKeyFromUrl(string url)
        {
            using (var md5 = MD5.Create())
            {
                var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(url));
                return Convert.ToBase64String(hashBytes)
                              .Replace("+", "")
                              .Replace("/", "")
                              .Replace("=", "");
            }
        }
        public static bool IsUrl(string pathOrUrl)
        {
            return pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        public static string GetFileExtension(string pathOrUrl)
        {
            if (IsUrl(pathOrUrl))
            {
                Uri uri = new Uri(pathOrUrl);
                return Path.GetExtension(uri.AbsolutePath).ToLowerInvariant().TrimStart('.');
            }
            else
            {
                return Path.GetExtension(pathOrUrl).ToLowerInvariant().TrimStart('.');
            }
        }

        public static IEnumerable<string> SplitToChunks(string text, int chunkMaxWords, int chunkOverlapWords)
        {
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int totalWords = words.Length;
            int start = 0;

            while (start < totalWords)
            {
                int length = Math.Min(chunkMaxWords, totalWords - start);
                var chunkWords = words.Skip(start).Take(length);
                var chunk = string.Join(" ", chunkWords);

                if (!string.IsNullOrWhiteSpace(chunk))
                {
                    yield return chunk.Trim();
                }

                start += (chunkMaxWords - chunkOverlapWords);
            }
        }

    }
}
