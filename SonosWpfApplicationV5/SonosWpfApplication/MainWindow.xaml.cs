using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.IO;
using System.Text.RegularExpressions;

namespace SonosWpfApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// UI setup and databinding inspired by this http://www.switchonthecode.com/tutorials/wpf-tutorial-using-the-listview-part-1
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<ZoneData> _ZoneCollection = new ObservableCollection<ZoneData>();
        ObservableCollection<PlaylistData> _PlaylistCollection = new ObservableCollection<PlaylistData>();
        ObservableCollection<ZoneData> _MasterZones = new ObservableCollection<ZoneData>();

        public MainWindow()
        {
            InitializeComponent();
        }
        public ObservableCollection<ZoneData> ZoneCollection
        { get { return _ZoneCollection; } }
        public ObservableCollection<PlaylistData> PlaylistCollection
        { get { return _PlaylistCollection; } }
        public ObservableCollection<ZoneData> MasterZones
        {  get { return _MasterZones; }}

        /// <summary>
        /// Processes the button click event when saving one playlist.
        /// </summary>
        private void playlistButton_Click(object sender, RoutedEventArgs e)
        {
            Button b = new Button();
            b = (Button)sender;
            string playlistQueue = ((PlaylistData)b.DataContext).PlaylistSQ;
            string playlistName = ((PlaylistData)b.DataContext).PlaylistName;

            string fileName = "sp_" + playlistName + ".xml";
            string content = UPnP.QueryDevice.GetPlaylist(playlistQueue, UPnP.QueryDevice.PlaylistAction.Save, 0);
            ExportPlaylist(fileName, content, "xml" /* type */);
        }

        private void M3UButton_Click(object sender, RoutedEventArgs e)
        {
            Button b = new Button();
            b = (Button)sender;
            string playlistQueue = ((PlaylistData)b.DataContext).PlaylistSQ;
            string playlistName = ((PlaylistData)b.DataContext).PlaylistName;

            string fileName = "m3u_" + playlistName + ".m3u";
            string content = UPnP.QueryDevice.GetPlaylist(playlistQueue, UPnP.QueryDevice.PlaylistAction.Save, 0);
            ExportPlaylist(fileName, content, "m3u" /* type */);
        }


        /// <summary>
        /// Processes the button click event when saving all playlists.
        /// </summary>
        private void SaveAll_Click(object sender, RoutedEventArgs e)
        {
            // Iterate through playlists.
            if (PlaylistCollection.Count > 0)
            {
                foreach (PlaylistData pd in PlaylistCollection)
                {
                    string fileName = "sp_" + pd.PlaylistName + ".xml";
                    string content = UPnP.QueryDevice.GetPlaylist(pd.PlaylistSQ, UPnP.QueryDevice.PlaylistAction.Save, 0 /* start at zero */);
                    ExportPlaylist(fileName, content, "xml" /* type */);
                }
                RefreshMessage("Saved all playlists.");
            }
        }

        /// <summary>
        /// Processes the import button click.
        /// Open dialog code inspired by http://www.kirupa.com/net/using_open_file_dialog_pg2.htm
        /// </summary>
        private void Import_Click(object sender, RoutedEventArgs e)
        {
            string playlistToImport = null;
            string playlistToImportSafe = null;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == true)
            {
                playlistToImport = ofd.FileName; // includes path
                playlistToImportSafe = ofd.SafeFileName; // no path
                playlistToImportSafe = System.IO.Path.GetFileNameWithoutExtension(playlistToImportSafe); // without extension
            }
            try
            {
                StringBuilder sb = new StringBuilder();
                using (var stream = new StreamReader(playlistToImport))
                {
                    RefreshMessage("Reading " + playlistToImport);
                    String line;
                    while ((line = stream.ReadLine()) != null)
                    {
                        sb.Append(line);
                    }
                }
                string DIDLString = sb.ToString();
                UPnP.QueryDevice.ImportPlaylist(DIDLString, ((ZoneData)MasterZoneDropDown.SelectedItem).ZoneAddress, playlistToImportSafe);
                RefreshMessage("Finished importing.");
            }
            catch (Exception ex)
            {
                RefreshMessage("Failed to read file " + playlistToImportSafe + " for import. " + ex.Message);
            }
        }

        /// <summary>
        /// Processes the discover button click.
        /// </summary>
        private void Discover_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshMessage("Discovering...");
                UPnP.Discovery.Discover();
                UPnP.QueryDevice.QueryZoneAttributes();
                UPnP.QueryDevice.FindMasters();
                UPnP.QueryDevice.QueryZonePlayerXml();
                UPnP.QueryDevice.GetPlaylists();
                RefreshMessage((UPnP.Discovery.Zones.Count != 0) ? 
                    "Discovered " + UPnP.Discovery.Zones.Count.ToString() + 
                    " devices. (" + DateTime.Now.ToString() + ")" : "No devices discovered. Try discovery again.");
                foreach (string zone in UPnP.Discovery.Zones)
                {
                    Uri uri = new Uri(zone);
                    ZoneData zd = new ZoneData
                    {
                        ZoneName = UPnP.Discovery.ZoneTable[zone],
                        ZoneAddress = uri.Host,
                        ZoneType = UPnP.Discovery.ZoneTypes[zone],
                        ZoneID = UPnP.Discovery.ZoneIDs[zone],
                        ZoneMaster = UPnP.Discovery.ZoneMasters[zone].ToString()
                    };
                    if (!_ZoneCollection.Contains(zd, new ZoneComparer()))
                    {
                        _ZoneCollection.Add(zd);
                    }
                    if (UPnP.Discovery.ZoneMasters[zone] && !_MasterZones.Contains(zd, new ZoneComparer()))
                    {
                        _MasterZones.Add(zd);
                    }
                }
                foreach (KeyValuePair<string, string> kvp in UPnP.QueryDevice.Playlists)
                {
                    PlaylistData pd = new PlaylistData
                    {
                        PlaylistName = kvp.Value,
                        PlaylistSQ = kvp.Key,
                        NumItems = UPnP.QueryDevice.GetPlaylist(kvp.Key, UPnP.QueryDevice.PlaylistAction.Count, 0  /* not used for count */)
                    };
                    if (!_PlaylistCollection.Contains(pd, new PlaylistComparer()))
                    {
                        _PlaylistCollection.Add(pd);
                    }
                }
            }
            catch(Exception ex)
            {
                RefreshMessage( ex.Message + " Try discovery again.");
            }

        }
        /// <summary>
        /// Gets a valid file name, making sure not bad characters get in the name.
        /// Code from: http://stackoverflow.com/questions/309485/c-sanitize-file-name
        /// </summary>
        private static string GetValidFileName(string name)
        {
            string invalidChars = Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidReStr = string.Format(@"[{0}]+", invalidChars);
            return Regex.Replace(name, invalidReStr, "_");
        }

        /// <summary>
        /// Exports a playlist.
        /// </summary>
        /// <param name="fileName">The name of the exported playlist file.</param>
        /// <param name="content">The content to go inside the file.</param>
        private void ExportPlaylist(string fileName, string content, string type)
        {
            string currDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = currDirectory + GetValidFileName(fileName);
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create)) // Synchronous mode
                {
                    if (type.Equals("xml"))
                    {
                        byte[] info = new UTF8Encoding(true).GetBytes(content);
                        fileStream.Write(info, 0, info.Length);
                        RefreshMessage("Saved " + fileName);
                    }
                    else
                    {
                        String playlist = UPnP.QueryDevice.CreateM3UPlaylist(content);
                        byte[] info = new UTF8Encoding(true).GetBytes(playlist);
                        fileStream.Write(info, 0, info.Length);
                        RefreshMessage("Saved " + fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                RefreshMessage("Failed to save " + fileName + ". " + ex.Message);
            }
        }

        /// <summary>
        /// Helper method to avoid typing the same two lines many times.
        /// </summary>
        /// <param name="message">Message to assign to UI.</param>
        public void RefreshMessage(string message)
        {
            Message.Text = message;
            Message.Refresh();
        }

    }

    /// <summary>
    /// Compares two zones to determine if they are equal.
    /// </summary>
    public class ZoneComparer : IEqualityComparer<ZoneData>
    {
        public bool Equals(ZoneData zd1, ZoneData zd2)
        {
            return zd1.ZoneID == zd2.ZoneID;
        }
        public int GetHashCode(ZoneData zd)
        {
            return zd.ZoneID.GetHashCode();
        }
    }

    /// <summary>
    /// Compares two playlists to determine if they are equal.
    /// </summary>
    public class PlaylistComparer : IEqualityComparer<PlaylistData>
    {
        public bool Equals(PlaylistData pd1, PlaylistData pd2)
        {
            return pd1.PlaylistSQ == pd2.PlaylistSQ;
        }
        public int GetHashCode(PlaylistData pd)
        {
            return (pd.PlaylistName + pd.PlaylistSQ).GetHashCode();
        }
    }

    /// <summary>
    /// Extension method to deal with refreshing UI element in WPF xaml.
    /// Idea came from here: http://social.msdn.microsoft.com/forums/en-US/wpf/thread/878ea631-c76e-49e8-9e25-81e76a3c4fe3
    /// </summary>
    public static class ExtensionMethods
    {
        private static Action EmptyDelegate = delegate() { };
        public static void Refresh(this UIElement uiElement)
        {
            uiElement.Dispatcher.Invoke(EmptyDelegate, System.Windows.Threading.DispatcherPriority.Render);
        }
    }

    /// <summary>
    /// Defines one zone.
    /// </summary>
    public class ZoneData
    {
        public string ZoneName { get; set; }
        public string ZoneAddress { get; set; }
        public string ZoneType { get; set; }
        public string ZoneID { get; set; }
        public string ZoneMaster { get; set; }
    }

    /// <summary>
    /// Defines one playlist.
    /// </summary>
    public class PlaylistData
    {
        public string PlaylistName { get; set; }
        public string PlaylistSQ { get; set; }
        public string NumItems { get; set; }
    }
}
