﻿using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


namespace Ya.Disk.Api
{
    class Program
    {

        public static IConfiguration Configuration;
        static async Task Main(string[] args)
        {

            Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true, true)
            .Build();



            var yaDiskApi = new YaDiskApi(Configuration);


            foreach (var arg in args)
            {
                Console.WriteLine(arg);
            }


            var localDirectory = "";
            var folderYaDisk = "";

            ConsoleKeyInfo cki = new ConsoleKeyInfo();
            do
            {
                switch (args.Length)
                {

                    case 0:
                        {
                            Console.WriteLine("Введите директорию с которой нужно скопировать файлы:");
                            localDirectory = Console.ReadLine();

                            Console.WriteLine("Введите директорию на Яндекс.Диске:");
                            folderYaDisk = Console.ReadLine();
                        }
                        break;

                    case 2:
                        {
                            localDirectory = args[0];
                            folderYaDisk = args[1];
                        }
                        break;
                    default:
                        Console.WriteLine(@"параметры введены некорректно, пример: D:\test test");
                        Environment.Exit(0);
                        break;

                }

                if (!yaDiskApi.CheckInputtData(localDirectory, folderYaDisk) && args.Length == 2)
                {
                    Environment.Exit(0);
                }


                if (!yaDiskApi.CheckInputtData(localDirectory, folderYaDisk))
                {
                    continue;
                }


                var allFiles = Directory.GetFiles($"\"{localDirectory}\"");

                var tasks = new List<Task<byte[]>>(allFiles.Length);

                foreach (var item in allFiles)
                {
                    tasks.Add(Task.Run(() => yaDiskApi.UploadFileToYaDiskAsync($"\"{folderYaDisk}\"", $"\"{item}\"")));
                    Console.WriteLine($"В очередь на загрузку добавлен файл:{Path.GetFileName(item)}");
                }

                await Task.WhenAll(tasks);

                Console.WriteLine("Для выхода нажмите Esc");

                cki = Console.ReadKey();
            }

            while (cki.Key != ConsoleKey.Escape);
        }

    }
}

