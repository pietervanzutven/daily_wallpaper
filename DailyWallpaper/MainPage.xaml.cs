﻿using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.UserProfile;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

namespace DailyWallpaper
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        public MainPage()
        {
            this.InitializeComponent();

            Windows.Foundation.Rect bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            double screenratio = bounds.Width / bounds.Height;

            this.NavigationCacheMode = NavigationCacheMode.Required;

            RefreshWallpaper(screenratio);
        }

        public async void RefreshWallpaper(double screenratio)
        {
            StatusText.Text = "Initializing...";
            StorageFile startupFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("startup.dwp", CreationCollisionOption.OpenIfExists);
            string domain = "http://www.bing.com";
            Uri domainuri = new Uri(domain);
            HttpClient httpClient = new HttpClient();
            string responsetext = "";

            bool success = false;
            while (!success)
            {
                try
                {
                    StatusText.Text = "Contacting Bing...";
                    HttpResponseMessage response = await httpClient.GetAsync(domainuri);
                    responsetext = await response.Content.ReadAsStringAsync();
                    
                    StatusText.Text = "Retrieving image description...";
                    int startindex = responsetext.IndexOf("\"copyright\"") + 13;
                    int endindex = responsetext.IndexOf("\"copyrightlink\"", startindex) - 2;
                    DescriptionText.Text = Regex.Unescape(responsetext.Substring(startindex, endindex - startindex));

                    StatusText.Text = "Retrieving image path...";
                    string subdomain = "/th?id=OHR.";
                    startindex = responsetext.IndexOf(subdomain) + 11;
                    endindex = responsetext.IndexOf(".jpg", startindex) - 9;
                    string figurename = responsetext.Substring(startindex, endindex - startindex);
                    string extension = ".jpg";

                    StatusText.Text = "Retrieving image size...";
                    string figuresize1 = "1920x1080";
                    string figuresize2 = "1920x1200";
                    if (screenratio < (1920.0 / 1200.0 + 1920.0 / 1080.0) / 2.0)
                    {
                        figuresize1 = "1920x1200";
                        figuresize2 = "1920x1080";
                    }
                    Uri figureuri = new Uri(domain + subdomain + figurename + figuresize1 + extension);

                    StatusText.Text = "Downloading image...";
                    StorageFile figurepath = await ApplicationData.Current.LocalFolder.CreateFileAsync(figurename + extension, CreationCollisionOption.OpenIfExists);
                    BackgroundDownloader downloader = new BackgroundDownloader();
                    DownloadOperation download = downloader.CreateDownload(figureuri, figurepath);
                    try
                    {
                        await download.StartAsync();
                    }
                    catch
                    {
                        figureuri = new Uri(domain + subdomain + figurename + figuresize2 + extension);
                        download = downloader.CreateDownload(figureuri, figurepath);
                        await download.StartAsync();
                    }

                    StatusText.Text = "Setting image preview...";
                    BitmapImage myimage = new BitmapImage(figureuri);
                    PreviewImage.Source = myimage;

                    StatusText.Text = "Setting wallpaper and lockscreen...";
                    await UserProfilePersonalizationSettings.Current.TrySetWallpaperImageAsync(figurepath);
                    await UserProfilePersonalizationSettings.Current.TrySetLockScreenImageAsync(figurepath);

                    success = true;

                    StatusText.Text = "Getting system accent color...";
                    Color accentColor = new UISettings().GetColorValue(UIColorType.Accent);

                    StatusText.Text = "Publishing system accent color...";
                    string json = "{\"color\":\"" + accentColor.R + "," + accentColor.G + "," + accentColor.B + "\"}";
                    HttpStringContent content = new HttpStringContent(json, UnicodeEncoding.Utf8, "application/json");
                    await httpClient.PutAsync(new Uri("https://api.myjson.com/bins/15n2pq"), content);
                }
                catch
                {
                    int counter = 60;
                    while (counter > 0)
                    {
                        StatusText.Text = "Error retrieving wallpaper, retrying in " + counter + "s...";
                        await Task.Delay(1000);
                        counter = counter - 1;
                    }
                }
                StatusText.Text = "Done!";
            }
        }
    }
}
