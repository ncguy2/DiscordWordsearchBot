using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace WordSearchBot.Core.Utils {
    public static class DownloadHelper {

        public static void DownloadTempFile(string url, Action<FileInfo> callback) {
            string downloadFile = DownloadFile(url, StringUtils.RandomString(8));
            callback(new FileInfo(downloadFile));
            File.Delete(downloadFile);
        }

        public static async void DownloadTempFileAsync(string url, Func<FileInfo, Task> callback) {
            string downloadFile = DownloadFile(url, StringUtils.RandomString(8));
            await callback(new FileInfo(downloadFile));
            File.Delete(downloadFile);
        }

        public static string DownloadFile(string url, string filename) {
            using WebClient client = new();
            Directory.CreateDirectory(".downloads");
            string filePath = $".downloads/{filename}.{StringUtils.GetFileExt(url)}";
            client.DownloadFile(url, filePath);

            return filePath;
        }
    }
}