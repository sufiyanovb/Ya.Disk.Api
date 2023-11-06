using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace Ya.Disk.Api
{
    public class YaDiskApi
    {
        private readonly IConfiguration _configuration;
        private readonly IProgress<string> _progress;

        public event Action<string> ErrorMessageHandler;

        public YaDiskApi(IConfiguration configuration, IProgress<string> progress = null)
        {
            _configuration = configuration;
            _progress = progress;
        }

        public async Task<bool> CheckInputtDataAsync(string localDirectory, string folderYaDisk)
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

            return await CheckFolderYaDiskAsync(folderYaDisk);

        }
        public async Task<bool> UploadFileToYaDiskAsync(string folderyaDisk, string fullPathToFile)
        {
            var urlForUploadFile = $"https://cloud-api.yandex.net/v1/disk/resources/upload?path=%2F{folderyaDisk}%2F{Path.GetFileName(fullPathToFile)}&overwrite=true";

            var responseMessage = await GetAsync(urlForUploadFile);

            var responseContent = await responseMessage.Content.ReadAsStringAsync();

            var responseJson = JsonSerializer.Deserialize<UploadFileResult>(responseContent);

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

            HttpResponseMessage response = await client.PostAsync(responseJson.Href, formData);

            _progress?.Report($"Файл: {Path.GetFileName(fullPathToFile)} {(response.IsSuccessStatusCode ? "" : " не ")} загружен");

            return response.IsSuccessStatusCode;

        }
        private async Task<bool> CheckFolderYaDiskAsync(string folderName)
        {
            var urlToCreateFolder = $"https://cloud-api.yandex.net/v1/disk/resources?path=%2F{folderName}";

            var responseMessage = await PutAsync(urlToCreateFolder);

            return responseMessage.StatusCode == HttpStatusCode.Conflict || responseMessage.StatusCode == HttpStatusCode.Created;//если папка уже создана или уже существует

        }

        public async Task<HttpResponseMessage> PutAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", $"{_configuration["token"]}");

                return await client.PutAsync(url, new StringContent(""));
            }
        }

        public async Task<HttpResponseMessage> GetAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", $"{_configuration["token"]}");

                return await client.GetAsync(url);
            }
        }
    }
}


