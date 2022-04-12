using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace Ya.Disk.Api
{
    public class YaDiskApi
    {
        private readonly IConfiguration _configuration;
        private readonly IProgress<string> _progress;
        private readonly Dictionary<int, string> _apiResponses;

        public event Action<string> ErrorMessageHandler;

        public YaDiskApi(IConfiguration configuration, IProgress<string> progress = null)
        {
            _configuration = configuration;
            _progress = progress;
            _apiResponses = _configuration.GetSection("ApiResponses").GetChildren().ToDictionary(i => int.Parse(i.Key), i => i.Value);

        }

        public bool CheckInputtData(string localDirectory, string folderYaDisk)
        {
            if (string.IsNullOrWhiteSpace(folderYaDisk))
            {
                ErrorMessageHandler?.Invoke("Не введена директория на Яндекс.Диске!");
                return false;
            }

            if (string.IsNullOrWhiteSpace(localDirectory))
            {
                ErrorMessageHandler?.Invoke("Не введена локальная директория!");
                return false;
            }

            if (!Directory.Exists(localDirectory))
            {
                ErrorMessageHandler?.Invoke("Введенной локальной папки не существует!");
                return false;
            }

            if (Directory.GetFiles(localDirectory).Length == 0)
            {
                ErrorMessageHandler?.Invoke("В локальной папке отсутствуют файлы!");
                return false;
            }

            return CheckFolderYaDisk(folderYaDisk);

        }
        public async Task<bool> UploadFileToYaDiskAsync(string folderyaDisk, string fullPathToFile)
        {
            var urlForUploadFile = $"https://cloud-api.yandex.net/v1/disk/resources/upload?path=%2F{folderyaDisk}%2F{Path.GetFileName(fullPathToFile)}&overwrite=true";

            var responseObject = GetResponseObject<BaseResult>(urlForUploadFile, "Get");

            using FileStream file = new FileStream(fullPathToFile, FileMode.Open, FileAccess.Read);
            using HttpContent filePathContent = new StringContent(fullPathToFile);
            using HttpContent fileStreamContent = new StreamContent(file);
            using var client = new HttpClient();
            using var formData = new MultipartFormDataContent
            {
                { filePathContent, "filepath" },
                { fileStreamContent, "file", Path.GetFileName(fullPathToFile) }
            };

            _progress?.Report($"Файл: {Path.GetFileName(fullPathToFile)} загружается");

            HttpResponseMessage response = await client.PostAsync(responseObject.Href, formData);

            _progress?.Report($"Файл: {Path.GetFileName(fullPathToFile)} {(response.IsSuccessStatusCode ? "" : " не ")} загружен");

            return response.IsSuccessStatusCode;

        }
        private bool CheckFolderYaDisk(string folderName)
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
                {
                    return true;
                }
                else
                {
                    ErrorMessageHandler?.Invoke($"Ошибка:{_apiResponses[exceptionStatusCode]}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessageHandler?.Invoke($"{ex.Message}");
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


