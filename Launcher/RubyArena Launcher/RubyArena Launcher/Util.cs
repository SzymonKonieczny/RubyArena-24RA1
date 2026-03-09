using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RubyArena_Launcher
{
    public static class Util
    {
        public static readonly HttpClient client = new HttpClient()
        {
            Timeout = TimeSpan.FromMinutes(30)
        };
        public static bool IsValidHttpUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                return false;

            return uri.Scheme == Uri.UriSchemeHttp ||
                   uri.Scheme == Uri.UriSchemeHttps;
        }
       public static string GetStringOrDefault(JsonElement element, string propertyName, string defaultValue = "")
        {
            if (element.TryGetProperty(propertyName, out JsonElement value))
                return value.GetString() ?? defaultValue;

            return defaultValue;
        }
        public static string ReadFileToString(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            return File.ReadAllText(filePath);
        }
        public static void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        public static async Task DownloadFileAsync(string url, string path)
        {
            var data = await client.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(path, data);
        }
        public static void ExtractZip(string zipPath, string extractDirectory)
        {
            if(!Directory.Exists(extractDirectory))
                Directory.CreateDirectory(extractDirectory);

            ZipFile.ExtractToDirectory(zipPath, extractDirectory, true);
        }
        public static void DeleteAllFiles(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return;

             DirectoryInfo dir = new DirectoryInfo(directoryPath);

            foreach (FileInfo file in dir.GetFiles())
            {
                file.Delete();
            }

            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                subDir.Delete(true);
            }
        }
    }
}
