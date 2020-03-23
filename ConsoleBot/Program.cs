using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
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
        static TelegramBotClient bot;

        static string DefaultDownloadPath = "D://TGBot_Downloads//";

        static string start = "Приветствую тебя! \n Я могу:\n- архивировать отправленные тобой файлы \n- делать фото черно-белыми \n- искать для тебя музыку по названию";

        static void Main(string[] args)
        {

            string token = "1070622790:AAHB5PT4dFq3_BhMArCTBuaeMP3H1eu6cqk";

            //var proxy = new WebProxy
            //{
            //    Address = new Uri($"http://192.176.54.209:3128"),
            //    UseDefaultCredentials = false
            //};

            //var httpClientHandler = new HttpClientHandler { Proxy = proxy };

            //HttpClient hc = new HttpClient(httpClientHandler);

            bot = new TelegramBotClient(token);
            SetToDefault();

            bot.StartReceiving();
            
            Console.ReadKey();

            bot.StopReceiving();
        }

        private static void SetToDefault()
        { 
            bot.OnMessage += MessageListener;
        }


        private static async void MessageListener(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var message = e.Message;
            var chatId = e.Message.Chat.Id;
            
            await StartOfDefault(message);

        }

        private static async void SetToArhivateMode()
        {
            bot.OnMessage -= MessageListener;
            bot.OnMessage += ArhivationMode;
        }

        private static async Task ShowFuncButton(long chatId)
        {
            await SubscribeOnButtonEvents();

            var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup( new[]
            {
                new[]{ Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("Заархивировать") },
                new[]{ Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("Сделать фото черно-белым") },
                new[]{ Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("Найти песню по названию") }
            });
            await bot.SendTextMessageAsync(chatId, start, replyMarkup: keyboard);
        }

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
                }
                else if (ev.CallbackQuery.Data == "Найти песню по названию")
                {
                    await bot.SendTextMessageAsync(chatID, "Введи название песни");
                }
            };
        }

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
        }

        private static async void ArhivationMode(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var path = e.Message.Document.FileName;
            var fileId = e.Message.Document.FileId;
            var chatId = e.Message.Chat.Id;

            var filePath = DefaultDownloadPath + path;
            var endPath = filePath + ".zip";

            await Download(fileId, filePath);

            await Arhivate(filePath, endPath);
            
            await SendFile(endPath, chatId);

            bot.OnMessage -= ArhivationMode;
            SetToDefault();
        }

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

        private static async Task Download(string fileId, string path)
        {
            var file = await bot.GetFileAsync(fileId);
            
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                await bot.DownloadFileAsync(file.FilePath, fs);
                fs.Dispose();
            }
        }

        private static async Task SendFile(string path, long chatId)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                Telegram.Bot.Types.InputFiles.InputOnlineFile file = new Telegram.Bot.Types.InputFiles.InputOnlineFile(fs, path);
                await bot.SendDocumentAsync(chatId, file);                
            }
        }

    }
}
