namespace BackgroundTask
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using Windows.ApplicationModel.Background;
    using Windows.Foundation;
    using Windows.Storage;
    using Windows.Storage.Streams;
    using Windows.UI.Notifications;
    using Windows.Web.Http;

    public sealed class Updater : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral _deferral = taskInstance.GetDeferral();

            await QuoteAsync();

            _deferral.Complete();
        }

        public static IAsyncOperation<string> QuoteAsync()
        {
            return QuoteAsyncHelper().AsAsyncOperation();
        }

        public static void Register()
        {
            var taskName = "Quoter";
            var taskRegistered = false;

            foreach (var task in Windows.ApplicationModel.Background.BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == taskName)
                {
                    taskRegistered = true;
                    break;
                }
            }

            if (!taskRegistered)
            {
                var builder = new BackgroundTaskBuilder();

                builder.Name = taskName;
                builder.TaskEntryPoint = "BackgroundTask.Updater";
                builder.SetTrigger(new TimeTrigger(15, false));

                var task = builder.Register();
            }
        }

        private async static Task<string> QuoteAsyncHelper()
        {
            var container = ApplicationData.Current.LocalSettings.CreateContainer("OS", ApplicationDataCreateDisposition.Always);
            bool windowsPhone = bool.Parse(container.Values["OS"].ToString());

            var text = await GetQuote();

            var tileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWide310x150Text04);

            var tileTextAttributes = tileXml.GetElementsByTagName("text");
            tileTextAttributes[0].InnerText = text;

            var squareTileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare150x150Text04);

            var squareTileTextAttributes = squareTileXml.GetElementsByTagName("text");
            squareTileTextAttributes[0].AppendChild(squareTileXml.CreateTextNode(text));

            var node = tileXml.ImportNode(squareTileXml.GetElementsByTagName("binding").Item(0), true);
            tileXml.GetElementsByTagName("visual").Item(0).AppendChild(node);

            if (!windowsPhone)
            {
                var bigSquareTileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare310x310TextList02);

                var bigSquareTileTextAttributes = bigSquareTileXml.GetElementsByTagName("text");
                bigSquareTileTextAttributes[0].AppendChild(bigSquareTileXml.CreateTextNode(text));

                node = tileXml.ImportNode(bigSquareTileXml.GetElementsByTagName("binding").Item(0), true);
                tileXml.GetElementsByTagName("visual").Item(0).AppendChild(node);
            }

            var tileNotification = new TileNotification(tileXml);

            TileUpdateManager.CreateTileUpdaterForApplication().Update(tileNotification);

            container = ApplicationData.Current.LocalSettings.CreateContainer("quotestore", ApplicationDataCreateDisposition.Always);
            container.Values["quote"] = text;
            container.Values["date"] = DateTime.Now.ToString();

            return string.Empty;
        }

        private async static Task<string> GetQuote()
        {
            using (var stream = await (await StorageFile.GetFileFromApplicationUriAsync(new System.Uri("ms-appx:///data.json"))).OpenReadAsync())
            {
                var buffer = new byte[stream.Size].AsBuffer();

                dynamic content = JArray.Parse(DataReader.FromBuffer(await stream.ReadAsync(buffer, buffer.Length, InputStreamOptions.None)).ReadString(buffer.Length).Substring(1));

                return JObject.Parse(content[new Random().Next(content.Count)].ToString()).text;
            }
        }
    }
}
