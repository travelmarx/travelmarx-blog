package travelmarx;

import java.io.BufferedReader;
import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;

import java.io.InputStreamReader;
import java.io.OutputStreamWriter;

import java.net.HttpURLConnection;
import java.net.MalformedURLException;
import java.net.ProtocolException;
import java.net.URL;
import java.net.URLDecoder;

import org.apache.commons.lang3.StringEscapeUtils;

/**
* Class responsible for extracting current Sonos queue to playlist and text
* file.
*/

public class PlayListExtractor {
 public PlayListExtractor(String ipAddress, String filePath, String playListName)
   throws MalformedURLException, ProtocolException, IOException {
   // Build HTTP request with SOAP envelope asking for details about the
   // current queue.
   URL url = new URL("http://" + ipAddress + ":1400/MediaServer/ContentDirectory/Control");
   HttpURLConnection request = (HttpURLConnection)url.openConnection();
  
   request.setRequestMethod("POST");
   request.addRequestProperty("SOAPACTION", "\"urn:schemas-upnp-org:service:ContentDirectory:1#Browse\"");
   request.setDoOutput(true);
   request.setReadTimeout(10000);
  
   request.connect();
  
   OutputStreamWriter input = new OutputStreamWriter(request.getOutputStream());
   input.write("<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n");
   input.write("<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">\r\n");
   input.write("  <s:Body>\r\n");
   input.write("    <u:Browse xmlns:u=\"urn:schemas-upnp-org:service:ContentDirectory:1\">\r\n");
   input.write("      <ObjectID>Q:0</ObjectID>\r\n");
   input.write("      <BrowseFlag>BrowseDirectChildren</BrowseFlag>\r\n");
   input.write("      <Filter>upnp:artist,dc:title</Filter>\r\n");
   input.write("     <StartingIndex>0</StartingIndex>\r\n");
   input.write("     <RequestedCount>100</RequestedCount>\r\n");
   input.write("     <SortCriteria></SortCriteria>\r\n");
   input.write("    </u:Browse>\r\n");
   input.write("  </s:Body>\r\n");
   input.write("</s:Envelope>\r\n");
   input.flush();

   // Read entire HTTP response, which is assumed to be in UTF-8.
  BufferedReader output = new BufferedReader(new InputStreamReader(request.getInputStream(), "UTF-8"));
   String response = new String();
   String line;
  
   while ((line = output.readLine()) != null) {
     response += line + "\r\n";
   }

   // Open output files, both in ISO8859-1 encoding.
   OutputStreamWriter txt = new OutputStreamWriter(new FileOutputStream(new File(filePath + "/" + playListName + ".txt")), "8859_1");
   OutputStreamWriter m3u = new OutputStreamWriter(new FileOutputStream(new File(filePath + "/" + playListName + ".m3u")), "8859_1");
  
   int i = 0, j = 0, k = 0, l = 0;
   i = response.indexOf("&lt;item", j);
   while (i >= 0) {
     // Loop over all items, where each item is a track on the queue.
     j = response.indexOf("&lt;/item&gt;", i);
     String track = response.substring(i + 8, j);
     String trackNo, artist, title, unc;

     trackNo = extract(track, " id=&quot;Q:", "&quot;");
     trackNo = trackNo.substring(trackNo.indexOf('/') + 1);
     unc = URLDecoder.decode(extract(track, "&gt;x-file-cifs:", "&lt;").replace('/', '\\').replaceAll("%20", " "), "UTF-8");
     artist = extract(track, "&lt;dc:creator&gt;", "&lt;/dc:creator&gt;");
     title = extract(track, "&lt;dc:title&gt;", "&lt;/dc:title&gt;");
    
     txt.append(decode(trackNo + ". " + artist + ": " + title) + "\r\n");
     m3u.append(decode(unc) + "\r\n");
    
     i = response.indexOf("&lt;item", j);
   }
   txt.close();
   m3u.close();
  
   request.disconnect();
 }

/**
* Extracts text surrounded by markers from given string.
* @param   s       String to extract text from.
* @param   start   Start marker.
* @param   stop    Stop marker.
* @return  Extracted text found between start and stop markers, markers
*          excluded.
*/

 private String extract(String s, String start, String stop) {
   int i = s.indexOf(start) + start.length();
  
   return s.substring(i, s.indexOf(stop, i));
 }

/**
* Decodes HTML character entities. First changes &amp; to &, then uses Apache
* Commons Lang to decode the standard entities and then manually decodes a non-
* standard entity (&apos;).
* @param     s     Text to be decoded.
* @return    Text with HTML character entities decoded.
*/

 private String decode(String s) {
   // Convert &amp; to &, &apos; to ' and let Apache Commons Lang about the rest.
   return StringEscapeUtils.unescapeHtml3(s.replaceAll("&amp;", "&")).replaceAll("&apos;", "'");
 }

/**
* Extracts current Sonos queue and saves track information to a playlist file
* and a text file. The playlist file is saved in .m3u format and the text file
* is a plain text file with each line in the format
* <track_no>. <artist>: <title>
* Both files are in ISO8859-1 format.
* @param   args    0: Sonos master IP address.
*                  1: Export file path.
*                  2: Playlist name.
* @throws  MalformedURLException
* @throws  ProtocolException
* @throws  IOException
*/

 public static void main(String[] args)
   throws MalformedURLException, ProtocolException, IOException {
   if (args.length < 3) {
     System.err.println("Usage: PlayListExtractor sonos_master_ip_address export_file_path playlist_name");
     System.exit(0);
   }
   PlayListExtractor pE = new PlayListExtractor(args[0], args[1], args[2]);
 }
}
