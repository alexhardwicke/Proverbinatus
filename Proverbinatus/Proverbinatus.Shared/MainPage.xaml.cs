using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
#if !WINDOWS_PHONE_APP
using Windows.UI.ApplicationSettings;
#endif
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using BackgroundTask;
using Windows.Storage;
using Windows.ApplicationModel.Background;
using System.Text;
using Windows.UI.Popups;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Proverbinatus
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private DateTime awaitLastUpdated = new DateTime();

        public MainPage()
        {
            var container = ApplicationData.Current.LocalSettings.CreateContainer("OS", ApplicationDataCreateDisposition.Always);

            this.InitializeComponent();

#if WINDOWS_PHONE_APP
            container.Values["OS"] = true.ToString();
#else
            container.Values["OS"] = false.ToString();
            this.uxAppBar.IsOpen = true;
            this.uxAbout.Visibility = Visibility.Collapsed;
            SettingsPane.GetForCurrentView().CommandsRequested += (_, args) =>
                {
                    var serverSettings =
                    new SettingsCommand(
                        "About",
                        "About",
                        (handler) =>
                        {
                            var aboutFlyout = new AboutFlyout();
                            aboutFlyout.Show();
                        });

                    args.Request.ApplicationCommands.Add(serverSettings);
                };
#endif

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await CheckForUpdate();

            await TimeChecker();
        }

        private async Task TimeChecker()
        {
            while (true)
            {
                await Task.Delay(1000);
                await CheckForUpdate();
            }
        }

        private async Task CheckForUpdate()
        {
            if ((DateTime.Now - this.awaitLastUpdated).TotalMinutes > 15)
            {
                Updater.Register();
                await GenerateQuote();
            }
            else
            {
                var date = DateTime.Parse(ApplicationData.Current.LocalSettings.Containers["quotestore"].Values["date"].ToString());
                if ((date - this.awaitLastUpdated).TotalMilliseconds > 0)
                {
                    var text = ApplicationData.Current.LocalSettings.Containers["quotestore"].Values["quote"].ToString();
                    uxProverbinatus.Text = text;
                    this.awaitLastUpdated = date;
                }
            }
        }

        private async Task GenerateQuote()
        {
            await Updater.QuoteAsync();
            var text = ApplicationData.Current.LocalSettings.Containers["quotestore"].Values["quote"].ToString();
            uxProverbinatus.Text = text;
            this.awaitLastUpdated = DateTime.Now;
        }

        private async void Refresh(object sender, RoutedEventArgs e)
        {
            uxFlipperHide.Begin();
            await Task.Delay(300);
            await GenerateQuote();
            uxFlipperShow.Begin();
        }

        private void About(object sender, RoutedEventArgs e)
        {
#if WINDOWS_PHONE_APP
            this.Frame.Navigate(typeof(AboutPage));
#endif
        }

        private void CloseHandler(object sender, object e)
        {
#if !WINDOWS_PHONE_APP
            this.uxAppBar.IsOpen = true;
#endif
        }

        private async void BackgroundTaskStatus(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();
            var tasks = BackgroundTaskRegistration.AllTasks;

            sb.AppendLine(string.Format("Number registered tasks: {0}", tasks.Count));

            foreach (var task in tasks)
            {
                var guid = task.Key;
                var taskreg = task.Value;
                var name = taskreg.Name;

                sb.AppendLine(string.Format("Task GUID: {0}", guid.ToString()));
                sb.AppendLine(string.Format("Task Name: {0}", name));
            }

            var box = new MessageDialog(sb.ToString());
            await box.ShowAsync();
        }
    }
}
