using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Services;

[WebService(Namespace = "http://solarmarx.com/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
[System.Web.Script.Services.ScriptService]
public class WebService : System.Web.Services.WebService {

    public WebService () {

        //Uncomment the following line if using designed components 
        //InitializeComponent(); 
    }

    [WebMethod]
    public string HelloWorld() {
        return "Hello World";
    }

    [WebMethod]
    public string GetEnphaseStats()
    {
        String key = "YOUR KEY GOES HERE";
        String uri = "https://api.enphaseenergy.com/api/systems/YOUR SYS ID/summary?key=" + key;
        WebRequest wrGETURL = WebRequest.Create(uri);

        Stream objStream = wrGETURL.GetResponse().GetResponseStream();

        StreamReader objReader = new StreamReader(objStream);

        StringBuilder sb = new StringBuilder();
        String sLine = "";

        while (sLine != null)
        {
            sLine = objReader.ReadLine();
            if (sLine != null)
                sb.Append(sLine);
        }
        return sb.ToString();
    }
    
}
