using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RubyArena_Launcher
{
    public static class Util
    {
        private static readonly HttpClient client = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        public static string ReadFileToString(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            return File.ReadAllText(filePath);
        }
        public static async Task<string> ReadTextFileFromUrl(string url)
        {
            client.Timeout = TimeSpan.FromSeconds(2);
            return await client.GetStringAsync(url);
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
            ZipFile.ExtractToDirectory(zipPath, extractDirectory, true);
        }
        public static void DeleteAllFiles(string directoryPath)
        {
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
