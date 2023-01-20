using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Runtime.ConstrainedExecution;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using Isopoh.Cryptography.Argon2;
using System.Diagnostics;

namespace MC_Launcher
{
    public partial class MainWindow : Window
    {
        string playerName = "Error", selectedVersion = "Lade Versionen...";
        bool loggedIn;
        public string rootPath, newestGameVersion;
        private HttpClient client;

        //[...]

        //Der Skin des angemeldeten Accounts wird von der Datenbank geladen und von Text zu Bitmap konvertiert.
        async void LoadSettings()
        {
            var values = new Dictionary<string, string>
            {
                { "accountname", playerName }
            };

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync("http://fakecraftsite.000webhostapp.com/GetSkin.php", content);
            string skin = await response.Content.ReadAsStringAsync();

            if (skin == "default")
            {
                SkinImage.Source = new BitmapImage(new Uri("pack://application:,,,/Media/steve.png"));
            }
            else
            {
                var stream = new MemoryStream(Convert.FromBase64String(skin));
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    SkinImage.Source = bitmap;

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        //Der Spieler kann einen neuen Skin über den Launcher hochladen.
        private async void ChangeSkinButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.Filter = "Bild-Dateien (*.png, *.jpeg, *.jpg)|*.png;*.jpeg;*.jpg";
            dialog.Title = "Skin auswählen";
            if (dialog.ShowDialog() == true)
            {
                BitmapImage img = new BitmapImage(new Uri(dialog.FileName));

                if (img.Width != 64 || img.Height != 64)
                {
                    MessageBox.Show("Die Skin-Datei muss 64x64 Pixel groß sein!");
                }
                else
                {
                    string s;
                    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(img));
                    using (MemoryStream ms = new MemoryStream())
                    {
                        encoder.Save(ms);
                        s = Convert.ToBase64String(ms.ToArray());
                    }

                    var values = new Dictionary<string, string>
                    {
                        { "skin", s },
                        { "accountname", playerName }
                    ;

                    var content = new FormUrlEncodedContent(values);
                    var response = await client.PostAsync("http://fakecraftsite.000webhostapp.com/SetSkin.php", content);
                    var responseString = await response.Content.ReadAsStringAsync();
                    if (responseString.StartsWith("Error"))
                    {
                        MessageBox.Show(responseString);
                    }
                    else
                    {
                        LoadSettings();
                    }
                }
            }
        }
    }
}
