﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace ConsoleBot
{
    class Program
    {
        static MyTelegramBot bot;

        static string DefaultDownloadPath = "D://TGBot_Downloads//";
        static string token = "1070622790:AAHB5PT4dFq3_BhMArCTBuaeMP3H1eu6cqk";

        static void Main()
        {
            //var proxy = new WebProxy
            //{
            //    Address = new Uri($"http://192.176.54.209:3128"),
            //    UseDefaultCredentials = false
            //};

            //var httpClientHandler = new HttpClientHandler { Proxy = proxy };

            //HttpClient hc = new HttpClient(httpClientHandler);

            bot = new MyTelegramBot(token, DefaultDownloadPath);

            bot.Start();
            
            Console.ReadKey();

            bot.Stop();
        }

        /*

        /// <summary>
        /// Подписывает дефлотный обработчик события получения сообщения ботом
        /// </summary>
        private static void SetToDefault()
        { 
            bot.OnMessage += MessageListener;
        }

        /// <summary>
        /// Дефолтный обработчик события получения сообщения ботом
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static async void MessageListener(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var message = e.Message;
            var chatId = e.Message.Chat.Id;
            
            await StartOfDefault(message);

        }

        /// <summary>
        /// Подписывет режим архивации на событие получения сообщения ботом и отписывает дефолтный обработчик
        /// </summary>
        private static async void SetToArhivateMode()
        {
            bot.OnMessage -= MessageListener;
            bot.OnMessage += ArhivationMode;
        }

        /// <summary>
        /// Подписывает режим обработки фотографии на событие получения сообщени ботом и отписывает дефолтный обработчик
        /// </summary>
        private static async void SetToImageMode()
        {
            bot.OnMessage -= MessageListener;
            bot.OnMessage += ImageMode;
        }

        /// <summary>
        /// Отправляет кнопки в част
        /// </summary>
        /// <param name="chatId">ID чата</param>
        /// <returns></returns>
        private static async Task ShowFuncButton(long chatId)
        {
            await SubscribeOnButtonEvents();

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup( new[]
            {
                new[]{ Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("Заархивировать") },
                new[]{ Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("Сделать фото черно-белым") },
                //new[]{ Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("Найти песню по названию") }
            });
            await bot.SendTextMessageAsync(chatId, start, replyMarkup: keyboard);
        }

        /// <summary>
        /// Создает обработчик события нажатия на кнопки
        /// </summary>
        /// <returns></returns>
        private static async Task SubscribeOnButtonEvents()
        {
            bot.OnCallbackQuery += async (object sc, Telegram.Bot.Args.CallbackQueryEventArgs ev) =>
            {
                var message = ev.CallbackQuery.Message;
                var chatID = message.Chat.Id;
                if (ev.CallbackQuery.Data == "Заархивировать")
                {
                    await bot.SendTextMessageAsync(chatID, "Пришли мне файл, который я должен заархивировать)");
                    SetToArhivateMode();
                }
                else if (ev.CallbackQuery.Data == "Сделать фото черно-белым")
                {
                    await bot.SendTextMessageAsync(chatID, "Пришли мне фото, которое я должен сделать черно-белым)");
                    SetToImageMode();
                }
            };
        }

        /// <summary>
        /// Обрабатывет действия /start иначе выполняет дефолтные действия
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private static async Task StartOfDefault(Telegram.Bot.Types.Message message)
        {
            var chatId = message.Chat.Id;

            if (message?.Type == Telegram.Bot.Types.Enums.MessageType.Text)
            {
                if (message.Text.Equals("/start") || message.Text.Equals("/info") || message.Text.Equals("/help"))
                {
                    await ShowFuncButton(chatId);//await bot.SendTextMessageAsync(message.Chat.Id, start);
                }
                else
                {
                    await bot.SendTextMessageAsync(chatId, message.Text);
                }
            }
            else
            {
                await bot.ForwardMessageAsync(chatId, chatId, message.MessageId);
            }
        }

        /// <summary>
        /// Режим обработки изображения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static async void ImageMode(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var message = e.Message;
            var chatId = message.Chat.Id;

            if (e.Message.Type != Telegram.Bot.Types.Enums.MessageType.Photo)
            {
                await bot.SendTextMessageAsync(chatId, "К сожалению, черно-белым я могу только фото, если оно отправлено именно как фото, а не документом((((\n Попробуй еще раз!");
                bot.OnMessage -= ImageMode;
                SetToDefault();
                await ShowFuncButton(chatId);
                return;
            }

            var bgnPath = DefaultDownloadPath + "Begin" + chatId+".jpg";
            var endPath = DefaultDownloadPath + "End" + chatId + ".jpg";

            var fileId = message.Photo[message.Photo.Length - 1].FileId;

            await Download(fileId, bgnPath);

            Image bmp;

            using (FileStream fs = new FileStream(bgnPath, FileMode.Open))
            {
                bmp = Image.FromStream(fs);
                fs.Dispose();
            }

            await Delete(bgnPath);

            bmp = await ToGray((Bitmap)bmp);


            using(FileStream fs = new FileStream(endPath, FileMode.Create))
            {
                bmp.Save(fs, System.Drawing.Imaging.ImageFormat.Jpeg);
                fs.Dispose();
            }
          
            await SendPhoto(endPath, chatId);

            await Delete(endPath);

            bot.OnMessage -= ImageMode;

            SetToDefault();

            await ShowFuncButton(message.Chat.Id);

        }

        /// <summary>
        /// Делает изображения черно-белым
        /// </summary>
        /// <param name="bmp">Изображение</param>
        /// <returns></returns>
        private static async Task<Bitmap> ToGray(Bitmap bmp)
        {
            for (int row = 0; row < bmp.Width; row++)
            {
                for (int column = 0; column < bmp.Height; column++)
                {
                    var colorValue = bmp.GetPixel(row, column);
                    var averageValue = ((int)colorValue.R + (int)colorValue.B + (int)colorValue.G) / 3;
                    bmp.SetPixel(row, column, Color.FromArgb(averageValue, averageValue, averageValue));
                }
            }

            return bmp;
        }

        /// <summary>
        /// Режим архивации
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static async void ArhivationMode(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var chatId = e.Message.Chat.Id;
            
            if(e.Message.Type != Telegram.Bot.Types.Enums.MessageType.Document)
            {
                await bot.SendTextMessageAsync(chatId, "К сожалению,  я могу архивировать только документы(((\n Попробуй еще раз!");
                bot.OnMessage -= ArhivationMode;
                SetToDefault();
                await ShowFuncButton(chatId);
                return;
            }

            var path = e.Message.Document.FileName;
            var fileId = e.Message.Document.FileId;

            var filePath = DefaultDownloadPath + path;
            var endPath = filePath + ".zip";

            await Download(fileId, filePath);

            await Arhivate(filePath, endPath);
            
            await SendDocument(endPath, chatId);

            await Delete(filePath);

            await Delete(endPath);

            bot.OnMessage -= ArhivationMode;
            SetToDefault();

            await ShowFuncButton(chatId);
        }

        /// <summary>
        /// Архивация документа
        /// </summary>
        /// <param name="path">Путь к файлу для архивации</param>
        /// <param name="endPath">Путь для сохранения архивированного файла</param>
        /// <returns></returns>
        private static async Task Arhivate(string path, string endPath)
        {
            using (FileStream sourceStream = new FileStream(path, FileMode.OpenOrCreate))
            {
                // поток для записи сжатого файла
                using (FileStream targetStream = File.Create(endPath))
                {
                    // поток архивации
                    using (GZipStream compressionStream = new GZipStream(targetStream, CompressionMode.Compress))
                    {
                        sourceStream.CopyTo(compressionStream); // копируем байты из одного потока в другой                       
                    }
                }
            }
        }               

        /// <summary>
        /// Отправка фото пользователю
        /// </summary>
        /// <param name="path">Путь к файлу</param>
        /// <param name="chatId">ID чата</param>
        /// <returns></returns>
        private static async Task SendPhoto(string path, long chatId)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                Telegram.Bot.Types.InputFiles.InputOnlineFile file = new Telegram.Bot.Types.InputFiles.InputOnlineFile(fs, path);
                await bot.SendPhotoAsync(chatId, file);
                fs.Dispose();
            }
        }

        /// <summary>
        /// Отправка документа пользователю
        /// </summary>
        /// <param name="path">Путь к документу</param>
        /// <param name="chatId">ID чата</param>
        /// <returns></returns>
        private static async Task SendDocument(string path, long chatId)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                Telegram.Bot.Types.InputFiles.InputOnlineFile file = new Telegram.Bot.Types.InputFiles.InputOnlineFile(fs, path);
                await bot.SendDocumentAsync(chatId, file);
                fs.Dispose();
            }
        }

        /// <summary>
        /// Скачивание файла от пользователя
        /// </summary>
        /// <param name="fileId">id файла</param>
        /// <param name="path">путь для сохранения</param>
        /// <returns></returns>
        private static async Task Download(string fileId, string path)
        {
            var file = await bot.GetFileAsync(fileId);
            
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                await bot.DownloadFileAsync(file.FilePath, fs);
                fs.Dispose();
            }
        }

        /// <summary>
        /// Удаление файла
        /// </summary>
        /// <param name="path">Путьк  файлу для удаления</param>
        /// <returns></returns>
        private static async Task Delete(string path)
        {
            File.Delete(path);
        }

    */

    }
}
