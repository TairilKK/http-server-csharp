using System.Text;
using System.IO.Compression;
class Gzip
{
    public static byte[] CompressWithGzip(string text)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(text);
        using (var memoryStream = new MemoryStream())
        {
            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                gzipStream.Write(buffer, 0, buffer.Length);
            }
            return memoryStream.ToArray();
        }
    }

    public static string CompressString(string text)
    {
        byte[] compressed = CompressWithGzip(text);
        Console.WriteLine("###");
        Console.WriteLine(
            BitConverter.ToString(compressed).Replace("-", " ")
        );
        Console.WriteLine("###");
        return Encoding.UTF8.GetString(compressed);
    }
}
