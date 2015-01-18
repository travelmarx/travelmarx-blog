using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Xml;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq; // Be sure to set "Target framework:"  to non-client, e.g. framework 4.0.

namespace UPnP
{
    /// <summary>
    /// The Discovery class does the UDP broadcast and holds the Sonos topology info in various dictionaries.
    /// </summary>
    public class Discovery
    {
        // Discovery parameter that may need to be changed.
        static TimeSpan _timeout = new TimeSpan(0, 0, 0, 10); // fourth parameter is seconds
        static string _broadcastIP = "239.255.255.250"; 
        static int _broadcastPort = 1900;

        // Define properties and backing objects.
        public TimeSpan TimeOut 
        { get { return _timeout; } set { _timeout = value; }}
        static public Dictionary<string, string> ZoneTable 
        { get { return _zoneTable; } set { _zoneTable = value; }}
        static public Dictionary<string, string> ZoneTypes
        { get { return _zoneTypes; } set { _zoneTypes = value; }}
        static public Dictionary<string, string> ZoneIDs
        { get { return _zoneIDs; } set { _zoneIDs = value; }}
        static public Dictionary<string, bool> ZoneMasters
        { get { return _zoneMasters; } set { _zoneMasters = value; }}
        static public ArrayList Zones
        { get { return _zones; } set { _zones = value; }}

        static ArrayList _zones = new ArrayList();
        static Dictionary<string, string> _zoneTable = new Dictionary<string, string>();
        static Dictionary<string, string> _zoneTypes = new Dictionary<string, string>();
        static Dictionary<string, string> _zoneIDs = new Dictionary<string, string>();
        static Dictionary<string, bool> _zoneMasters = new Dictionary<string, bool>();

        /// <summary>
        /// Sends a UDP package over the specified broadcast IP and port and waits for responses.
        /// Some background: http://www.upnp-hacks.org/upnp.html, http://upnp.org/specs/arch/UPnP-arch-DeviceArchitecture-v1.0.pdf
        /// </summary>
        static public void Discover()
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            string req = "M-SEARCH * HTTP/1.1\r\n" +
            "HOST: " + _broadcastIP + ":" + _broadcastPort.ToString() + "\r\n" +
            "ST:upnp:rootdevice\r\n" +
            "MAN:\"ssdp:discover\"\r\n" +
            "MX:2\r\n\r\n" +
            "HeaderEnd: CRLF";
            byte[] data = Encoding.ASCII.GetBytes(req);
            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(_broadcastIP), _broadcastPort);
            byte[] buffer = new byte[0x8000];
            ArrayList responses = new ArrayList();

            DateTime start = DateTime.Now;

            // Are multiple sends needed? Is sending one broadcast sufficient?
            s.SendTo(data, ipe);

            int length = 0;
            do
            {
                if (s.Available > 0)
                {
                    length = s.Receive(buffer);
                    string resp = Encoding.ASCII.GetString(buffer, 0, length).ToLower();
                    responses.Add(resp);
                }
            } while (DateTime.Now.Subtract(start) < _timeout);
            s.Shutdown(SocketShutdown.Both);
            s.Close();

            // Go through results.

            for (int i = 0; i < responses.Count; i++)
            {
                String resp = responses[i].ToString();
                if (resp.ToLower().Contains("upnp:rootdevice") && resp.ToLower().Contains("sonos"))
                {
                    string loc = resp.Substring(resp.ToLower().IndexOf("location:") + 9);
                    loc = loc.Substring(0, loc.IndexOf("\r")).Trim();
                    if (!_zones.Contains(loc))
                    {
                        _zones.Add(loc);
                        _zoneTable.Add(loc, ""); // Will fill in zone friendly name later.
                    }
                }
            }

        }
    }
    /// <summary>
    /// The QueryDevice class does the following:
    /// 1. Queries (SOAP) zone attributes to get zone friendly name.
    /// 2. Queries (HTTP) zone player xml to get zone type and ID. 
    /// 3. Queries (SOAP) zone transports to see if zone is a master.
    /// 4. Queries (SOAP) zone content directory to get playlist list.
    /// 5. Queries (SOAP) zone content directory to get one playlist's content or a count of items.
    /// 6. 
    /// </summary>
    public static class QueryDevice
    {
        // Properties and backing objects.
        public static Dictionary<string, string> Playlists
        { get { return _playlists; } set { _playlists = value; } }
        public enum PlaylistAction { Count, Save};
        static public Dictionary<string, string> _playlists = new Dictionary<string, string>();
        static int _maxPlaylistPagingResults = 100;
        static int _maxPlaylistsReturned = 100;

        /// <summary>
        /// Get attributes for each zone in the Sonos topology.
        /// </summary>
        public static void QueryZoneAttributes()
        {
            if (UPnP.Discovery.ZoneTable.Count > 0)
            {
                foreach (string zone in UPnP.Discovery.Zones)
                {
                    // Query to get zone attributes.
                    string path = "/DeviceProperties/Control";
                    Uri uri = new Uri(zone);
                    string host = uri.Scheme + "://" + uri.Host + ":" + uri.Port.ToString();
                    string soapAction = "urn:upnp-org:serviceId:DeviceProperties#GetZoneAttributes";
                    string soapBody = "<u:GetZoneAttributes xmlns:u=\"urn:schemas-upnp-org:service:DeviceProperties:1\">"+
                                      "</u:GetZoneAttributes>";
                    XmlDocument resp = SOAPRequest(path, host, soapBody, soapAction);
                    string zoneName = resp.SelectSingleNode("//CurrentZoneName").InnerText;
                    UPnP.Discovery.ZoneTable[zone] = zoneName;
                }
            }
        }

        /// <summary>
        /// Gets the zoneplayer xml file to read two pieces of information, the zone ID and type. We
        /// could use this xml to figure out the friendly name of the zone but we do that elsewhere.
        /// </summary>
        public static void QueryZonePlayerXml()
        {
            if (UPnP.Discovery.ZoneTable.Count > 0)
            {
                foreach (string zone in UPnP.Discovery.Zones)
                {
                    if (!UPnP.Discovery.ZoneTable[zone].ToLower().Contains("bridge"))
                    {
                        string path = "/xml/device_description.xml"; //  "/xml/zone_player.xml";
                        Uri uri = new Uri(zone);
                        string host = uri.Scheme + "://" + uri.Host + ":" + uri.Port.ToString();
                        XmlDocument resp = GetWebResponse(path, host);

                        XmlNamespaceManager nsm = new XmlNamespaceManager(resp.NameTable);
                        nsm.AddNamespace("zp", "urn:schemas-upnp-org:device-1-0");
                        string zoneType = resp.SelectSingleNode("//zp:modelNumber", nsm).InnerText;
                        UPnP.Discovery.ZoneTypes[zone] = zoneType;
                        string uuid = resp.SelectSingleNode("//zp:UDN", nsm).InnerText;
                        UPnP.Discovery.ZoneIDs[zone] = uuid.Substring(5, uuid.Length - 5); // need to remove uuid:
                    }
                    else
                    {
                        UPnP.Discovery.ZoneIDs[zone] = "....";
                        UPnP.Discovery.ZoneTypes[zone] = "ZoneBridge";
                    }
                }
            }
        }

        /// <summary>
        /// Iterates through zones and figures out if the zone is a master.
        /// </summary>
        public static void FindMasters()
        {
            if (UPnP.Discovery.ZoneTable.Count > 0)
            {
                foreach (string zone in UPnP.Discovery.Zones)
                {
                    if (!UPnP.Discovery.ZoneTable[zone].ToLower().Contains("bridge"))
                    {
                        try
                        {
                            string path = "/MediaRenderer/AVTransport/Control";
                            Uri uri = new Uri(zone);
                            string host = uri.Scheme + "://" + uri.Host + ":" + uri.Port.ToString();
                            string soapAction = "uurn:schemas-upnp-org:service:AVTransport:1#GetPositionInfo";
                            string soapBody = "<u:GetPositionInfo xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\">" +
                                                  "<InstanceID>0</InstanceID>" +
                                                  "<Channel>Master</Channel>" +
                                              "</u:GetPositionInfo>";
                            XmlDocument resp = SOAPRequest(path, host, soapBody, soapAction);

                            string trackURI = resp.SelectSingleNode("//TrackURI").InnerText;
                            // check SOAP for TrackURI for x-rincon:RINCON
                            UPnP.Discovery.ZoneMasters[zone] = (!trackURI.StartsWith("x-rincon:RINCON") | trackURI == String.Empty);
                        }
                        catch (Exception e)
                        {
                            // It was reported that some devices throw an error as reported by a reader, adding try to catch this.
                            // Currently set message but don't use it. Consider surfacing in .xaml
                            string err = e.Message;
                            UPnP.Discovery.ZoneMasters[zone] = false;
                        }
                    }
                    else
                    {
                        UPnP.Discovery.ZoneMasters[zone] = false;
                    }
                }
            }

        }

        /// <summary>
        /// Gets all playlists defined for the Sonos topology.
        /// </summary>
        public static void GetPlaylists()
        {
            if (UPnP.Discovery.Zones.Count > 0)
            {
                string zone = UPnP.Discovery.Zones[0].ToString(); // use first in list if it isn't a bridge
                if (UPnP.Discovery.ZoneTable[zone].ToLower().Contains("bridge"))
                {
                    zone = UPnP.Discovery.Zones[1].ToString(); // use second
                }
                string path = "/MediaServer/ContentDirectory/Control";
                Uri uri = new Uri(zone);
                string host = uri.Scheme + "://" + uri.Host + ":" + uri.Port.ToString();
                string soapAction = "urn:schemas-upnp-org:service:ContentDirectory:1#Browse";
                string soapBody = "<u:Browse xmlns:u=\"urn:schemas-upnp-org:service:ContentDirectory:1\">" +
                                      "<ObjectID>SQ:</ObjectID>" + 
                                      "<BrowseFlag>BrowseDirectChildren</BrowseFlag>" + 
                                      "<Filter></Filter>" + 
                                      "<StartingIndex>0</StartingIndex>" +
                                      "<RequestedCount>" + _maxPlaylistsReturned.ToString() + "</RequestedCount>" + 
                                      "<SortCriteria></SortCriteria>" +
                                   "</u:Browse>";
                XmlDocument resp = SOAPRequest(path, host, soapBody, soapAction);

                XmlNamespaceManager nsm = new XmlNamespaceManager(resp.NameTable);
                nsm.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");
                XmlDocument resultNode = new XmlDocument();
                resultNode.LoadXml(resp.SelectSingleNode("*//Result").InnerText);
                XmlNodeList playlists = resultNode.SelectNodes("//dc:title", nsm);
                foreach (XmlNode xn in playlists)
                {
                    string id = xn.ParentNode.Attributes["id"].Value;
                    if (!_playlists.ContainsKey(id))
                    {
                        _playlists.Add(id, xn.InnerText);
                    }
                }
            }

        }

        /// <summary>
        /// Gets a playlist.
        /// </summary>
        /// <param name="playlistID">The playlist ID in the system, like "SQ:14" - for Saved Queue.</param>
        /// <param name="action">What to find out about the playlist, either return the items in the playlist or a count of the items.</param>
        /// <returns></returns>
        public static string GetPlaylist(string playlistID, PlaylistAction action, int index)
        {
            string zone = UPnP.Discovery.Zones[0].ToString(); // use first in list
            string path = "/MediaServer/ContentDirectory/Control";
            string numResults = String.Empty;
            string retVal = String.Empty;

            Uri uri = new Uri(zone);
            string host = uri.Scheme + "://" + uri.Host + ":" + uri.Port.ToString();
            string soapAction = "urn:schemas-upnp-org:service:ContentDirectory:1#Browse";
            string soapBody = "<u:Browse xmlns:u=\"urn:schemas-upnp-org:service:ContentDirectory:1\">" +
                                   "<ObjectID>" + playlistID + "</ObjectID>" +
                                   "<BrowseFlag>BrowseDirectChildren</BrowseFlag>" +
                                   "<Filter></Filter>" +
                                   "<StartingIndex>" + (_maxPlaylistPagingResults * (index)).ToString() + "</StartingIndex>" + 
                                   "<RequestedCount>" + (_maxPlaylistPagingResults).ToString() + "</RequestedCount>" + 
                                   "<SortCriteria></SortCriteria>" + 
                               "</u:Browse>";
            XmlDocument resp = SOAPRequest(path, host, soapBody, soapAction);

            XmlNamespaceManager nsm = new XmlNamespaceManager(resp.NameTable);
            nsm.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");
            XmlDocument resultNode = new XmlDocument();
            numResults = resp.SelectSingleNode("*//TotalMatches").InnerText;
            switch (action)
            {
                default:
                case PlaylistAction.Count:
                    retVal = numResults;
                    break;
                case PlaylistAction.Save:
                    if (Int32.Parse(numResults) > _maxPlaylistPagingResults*(index+1) /* zero-based index */)
                    {
                        index += 1;
                        // Get playlist items recursively.
                        XDocument recurVal = XDocument.Parse(GetPlaylist(playlistID, action, index));
                        XDocument currVal = XDocument.Parse(resp.SelectSingleNode("*//Result").InnerText);
                        currVal.Root.Add(recurVal.Root.Elements());
                        retVal = currVal.ToString();
                    }
                    else
                    {
                        retVal = resp.SelectSingleNode("*//Result").InnerText;
                    }
                    break;
            }
            return retVal;
        }

        public static string CreateM3UPlaylist(string content)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("#EXTM3U");
            sb.Append(System.Environment.NewLine);
            sb.Append(System.Environment.NewLine);

            // Then we iterate through the DIDL xml item by item
            string DIDLTemplate = "<DIDL-Lite xmlns:dc=\"http://purl.org/dc/elements/1.1/\" " +
                                    "xmlns:upnp=\"urn:schemas-upnp-org:metadata-1-0/upnp/\" " +
                                    "xmlns:r=\"urn:schemas-rinconnetworks-com:metadata-1-0/\" " +
                                    "xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\">{0}" +
                                   "</DIDL-Lite>";
            int musicTracksSent = 0;
            XmlDocument DIDLXml = new XmlDocument();
            DIDLXml.LoadXml(SanitizeXmlString(content));
            XmlNamespaceManager nsm = new XmlNamespaceManager(DIDLXml.NameTable);
            nsm.AddNamespace("didl", "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/");
            nsm.AddNamespace("dc", "http://purl.org/dc/elements/1.1/"); // not required for below currently
            nsm.AddNamespace("upnp", "urn:schemas-upnp-org:metadata-1-0/upnp/"); // not required for below currently
            XmlNodeList nl = DIDLXml.SelectNodes("//didl:item", nsm);
            foreach (XmlNode node in nl)
            {
                string location = node.SelectSingleNode("didl:res", nsm).InnerText;
                location = location.Remove(0, 12); // Remove x-file-cifs:
                location = HttpUtility.UrlDecode(location);
                string name = node.SelectSingleNode("dc:title", nsm).InnerText;
                string creator = "";
                if (node.SelectSingleNode("dc:creator", nsm) != null)
                {
                    creator = node.SelectSingleNode("dc:creator", nsm).InnerText;
                }
                string oneItemDIDL = HttpUtility.HtmlEncode(String.Format(DIDLTemplate, node.OuterXml));
                //oneItemDIDL = System.Security.SecurityElement.Escape(oneItemDIDL);
                sb.Append("#EXTINF:0," + creator + " - " + name);
                sb.Append(System.Environment.NewLine);
                sb.Append(location.Replace("/","\\"));
                sb.Append(System.Environment.NewLine);
                sb.Append(System.Environment.NewLine);

                musicTracksSent += 1;
                // Report on items written?
            }
            return sb.ToString();
        }

        /// <summary>
        /// Imports playlist content from a string into a master controller queue.
        /// </summary>
        /// <param name="content">The XML string that represents the playlist.</param>
        /// <param name="masterZoneAddress">The master zone address, like "192.168.2.5"</param>
        /// <param name="name">The name to give the playlist.</param>
        public static void ImportPlaylist(string content, string masterZoneAddress, string name)
        {
            // First we clear items in the queue
            string path = "/MediaRenderer/AVTransport/Control";
            string host = "http://" + masterZoneAddress + ":1400";
            string soapAction = "urn:schemas-upnp-org:service:AVTransport:1#RemoveAllTracksFromQueue";
            string soapBody = "<u:RemoveAllTracksFromQueue xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\">" + 
                                   "<InstanceID>0</InstanceID>" +
                               "</u:RemoveAllTracksFromQueue>";
            XmlDocument resp = SOAPRequest(path, host, soapBody, soapAction);

            // Then we iterate through the DIDL xml item by item
            string DIDLTemplate = "<DIDL-Lite xmlns:dc=\"http://purl.org/dc/elements/1.1/\" " + 
                                    "xmlns:upnp=\"urn:schemas-upnp-org:metadata-1-0/upnp/\" " +
                                    "xmlns:r=\"urn:schemas-rinconnetworks-com:metadata-1-0/\" " + 
                                    "xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\">{0}"+
                                   "</DIDL-Lite>";
            int musicTracksSent = 0;
            XmlDocument DIDLXml = new XmlDocument();
            DIDLXml.LoadXml(SanitizeXmlString(content));
            XmlNamespaceManager nsm = new XmlNamespaceManager(DIDLXml.NameTable);
            nsm.AddNamespace("didl", "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/");
            nsm.AddNamespace("dc", "http://purl.org/dc/elements/1.1/"); // not required for below currently
            nsm.AddNamespace("upnp", "urn:schemas-upnp-org:metadata-1-0/upnp/"); // not required for below currently
            XmlNodeList nl = DIDLXml.SelectNodes("//didl:item", nsm);
            foreach (XmlNode node in nl)
            {
                // Create a command
                string protocol = node.SelectSingleNode("didl:res", nsm).InnerText;
                string oneItemDIDL = HttpUtility.HtmlEncode(String.Format(DIDLTemplate, node.OuterXml));
                //oneItemDIDL = System.Security.SecurityElement.Escape(oneItemDIDL);
                string title = name;

                // Now put the element in the queue
                soapAction = "urn:schemas-upnp-org:service:AVTransport:1#AddURIToQueue";
                soapBody = "<u:AddURIToQueue xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\">" + 
                               "<InstanceID>0</InstanceID>" + 
                               "<EnqueuedURI>" + protocol + "</EnqueuedURI>" + 
                               "<EnqueuedURIMetaData>" + oneItemDIDL + "</EnqueuedURIMetaData>" + 
                               "<DesiredFirstTrackNumberEnqueued>0</DesiredFirstTrackNumberEnqueued>" + 
                               "<EnqueueAsNext>0</EnqueueAsNext>" + 
                            "</u:AddURIToQueue>";
                resp = SOAPRequest(path, host, soapBody, soapAction);
                musicTracksSent += 1;
                // Process return parameters to provide info about has been put into queue?
            }
            if (musicTracksSent > 0)  
            {
                // At least one thing was written so try to save. If program exits before this point then items
                // are in queue in master zone and can be manually saved.
                name = name.Substring(0, Math.Min(20, name.Length)); // Limit on playlist length.
                soapAction = "urn:schemas-upnp-org:service:AVTransport:1#SaveQueue";
                soapBody = "<u:SaveQueue xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\">" + 
                               "<InstanceID>0</InstanceID>" + 
                               "<Title>" + name + "</Title>" + 
                               "<ObjectID></ObjectID>" + 
                           "</u:SaveQueue>";
                resp = SOAPRequest(path, host, soapBody, soapAction);
            }

        }

        /// <summary>
        /// Gets the response to an HTTP request.
        /// </summary>
        /// <param name="path">URL path, like "/xml/zoneplayer.xml"</param>
        /// <param name="host">Host, like "192.168.2.5"</param>
        /// <returns></returns>
        private static XmlDocument GetWebResponse(string path, string host)
        {
            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(host + path);
            webReq.Method = "GET";

            //Get the response handle, no response yet.
            HttpWebResponse webResp = (HttpWebResponse)webReq.GetResponse();

            //Get response
            Stream strm = webResp.GetResponseStream();
            StreamReader strmReader = new StreamReader(strm);
            string data = strmReader.ReadToEnd();
            XmlDocument resp = new XmlDocument();
            resp.LoadXml(SanitizeXmlString(data));
            strmReader.Close();
            strm.Close();
            webResp.Close();
            return resp;
        }

        /// <summary>
        /// Gets the response to a HTTP request with SOAP body.
        /// </summary>
        /// <param name="path">URL path, like "/MediaRenderer/AVTransport/Control"</param>
        /// <param name="host">Host, like "192.168.2.5"</param>
        /// <param name="soapBody">The SOAP envelope containing the parameters of the request.</param>
        /// <param name="soapAction">The SOAP action, like "Browse" or "SaveQueue".</param>
        /// <returns></returns>
        private static XmlDocument SOAPRequest(string path, string host, string soapBody, string soapAction)
        {
            string reqBody = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
            "<s:Body>" +
            soapBody +
            "</s:Body>" +
            "</s:Envelope>";
            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(host + path);
            webReq.Method = "POST";
            byte[] buffer = Encoding.UTF8.GetBytes(reqBody);
            webReq.Headers.Add("SOAPACTION", "\"" + soapAction + "\"");
            webReq.ContentType = "text/xml; charset=\"utf-8\"";
            webReq.ContentLength = reqBody.Length;

            // Open  a stream for writing. Close.
            Stream postData = webReq.GetRequestStream();
            postData.Write(buffer, 0, buffer.Length);
            postData.Close();

            //Get the response handle, no response yet.
            HttpWebResponse webResp = (HttpWebResponse)webReq.GetResponse();

            //Get response
            int contentLength = (int)webResp.ContentLength;
            char[] data = new char[contentLength];
            Stream strm = webResp.GetResponseStream();
            StreamReader strmReader = new StreamReader(strm);
            int chunkSize = 256;
            if (contentLength <= chunkSize)
            {
                strmReader.Read(data, 0, contentLength);
            }
            else
            {
                int count = strmReader.Read(data, 0, chunkSize);
                int indx = count;
                while (count > 0)
                {
                    int inc = (contentLength - chunkSize) >= indx ? chunkSize : contentLength - indx;
                    count = strmReader.Read(data, indx, inc);
                    indx += count;
                }
            }
            XmlDocument resp = new XmlDocument();
            resp.LoadXml("<root>" + SanitizeXmlString(new String(data)) + "</root>");
            strmReader.Close();
            strm.Close();
            webResp.Close();
            return resp;
        }

        /// <summary>
        /// Removes junk from an XML string that doesn't belong there.
        /// Source: http://seattlesoftware.wordpress.com/2008/09/11/hexadecimal-value-0-is-an-invalid-character/
        /// Would be best to follow advice in link and build functionality into StreamReader.
        /// </summary>
        /// <param name="xml">XML to check.</param>
        /// <returns></returns>
        public static string SanitizeXmlString(string xml)
        {
            if (xml == null)
            {
                throw new ArgumentNullException("xml");
            }
            StringBuilder buffer = new StringBuilder(xml.Length);

            foreach (char c in xml)
            {
                if (XmlConvert.IsXmlChar(c))
                {
                    buffer.Append(c);
                }
            }
            return buffer.ToString();
        }
    }
}
