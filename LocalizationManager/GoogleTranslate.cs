using log4net;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;

namespace LocalizationManager
{
    public class GoogleTranslate : ITranslate
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(GoogleTranslate));

        public string Translate(string sourceLang, string targetLang, string sourceText)
        {
            string apiKey = ""; // private key
            string baseUrl = "https://www.googleapis.com/language/translate/v2?";
            string responseText = string.Empty;
                        
            try
            {
                HttpWebRequest request = WebRequest.Create(baseUrl + string.Format("key={0}&q={1}&source={2}&target={3}",
                    new object[] { apiKey, sourceText, sourceLang, targetLang })) as HttpWebRequest;
                request.Method = "GET";
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (JsonTextReader reader = new JsonTextReader(new StreamReader(response.GetResponseStream())))
                    {
                        while (reader.Read())
                        {
                            if ((reader.TokenType == JsonToken.PropertyName) && (reader.Value.ToString() == "translatedText"))
                            {
                                return reader.ReadAsString();
                            }
                        }
                    }
                    return sourceText;
                }
                return ("Failed with " + response.StatusCode);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }
            return responseText;
        }
    }
}