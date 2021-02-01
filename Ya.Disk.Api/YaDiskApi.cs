using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;


namespace Ya.Disk.Api
{
    public class YaDiskApi
    {
        private readonly IConfiguration _configuration;

        public YaDiskApi(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool CheckInputtData(string localDirectory, string folderYaDisk)
        {
            if (string.IsNullOrWhiteSpace(folderYaDisk))
            {
                Console.WriteLine("Не введена директория на Яндекс.Диске!");
                return false;
            }

            if (string.IsNullOrWhiteSpace(localDirectory))
            {
                Console.WriteLine("Не введена локальная директория!");
                return false;
            }

            if (!Directory.Exists(localDirectory))
            {
                Console.WriteLine("Введенной локальной папки не существует!");
                return false;

            }

            if (Directory.GetFiles(localDirectory).Length == 0)
            {
                Console.WriteLine("В локальной папке отсутствуют файлы!");
                return false;
            }

            return (CheckFolderYaDisk(folderYaDisk));

        }

        public async Task<byte[]> UploadFileToYaDiskAsync(string folderyaDisk, string fullPathToFile)
        {
            var urlForUploadFile = $"https://cloud-api.yandex.net/v1/disk/resources/upload?path=%2F{folderyaDisk}%2F{Path.GetFileName(fullPathToFile)}&overwrite=true";

            var responseObject = GetResponseObject<BaseResult>(urlForUploadFile, "Get");

            var uri = new Uri(responseObject.Href);

            using var client = new WebClient();

            client.UploadFileCompleted += (s, e) => { Console.WriteLine($"Файл : {Path.GetFileName(fullPathToFile)} загружен"); };
            client.UploadProgressChanged += (s, e) => { Console.WriteLine($"Файл : {Path.GetFileName(fullPathToFile)} загружается"); };

            var file = await client.UploadFileTaskAsync(uri, responseObject.Method, $"{fullPathToFile}");

            return file;
        }
        public bool CheckFolderYaDisk(string folderName)
        {
            try
            {

                var urlToCreateFolder = $"https://cloud-api.yandex.net/v1/disk/resources?path=%2F{folderName}";

                var responseObject = GetResponseObject<UploadFileResult>(urlToCreateFolder, "Put");

                return true;
            }
            catch (WebException ex)
            {
                var exceptionStatusCode = (int)((HttpWebResponse)ex.Response).StatusCode;
                if (exceptionStatusCode == 409)//если папка уже существует
                    return true;
                else
                {
                    Console.WriteLine($"Ошибка:{ApiResponse.AllResponses[exceptionStatusCode]}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
                return false;
            }
        }
        private T GetResponseObject<T>(string url, string httpMethod)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = httpMethod;
            request.Accept = "application/json";
            request.Headers["Authorization"] = $"OAuth {_configuration["token"]}";

            using HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using Stream responseStream = response.GetResponseStream();
            using StreamReader reader = new StreamReader(responseStream);
            var responseText = reader.ReadToEnd();

            return JsonSerializer.Deserialize<T>(responseText);
        }
    }

}


