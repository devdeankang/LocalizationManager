using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace LocalizationManager
{
    public class MicrosoftTranslate : ITranslate
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MicrosoftTranslate));

        public string Translate(string sourceLang, string targetLang, string sourceText)
        {
            string subscriptionKey = "1d09d88d138d4b119602d27e90b24229";
            string endpoint = "https://api.cognitive.microsofttranslator.com/";
            string location = "koreacentral";

            string route = string.Format("/translate?api-version=3.0&from={0}&to={1}", sourceLang, targetLang);
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint + route);
                request.Method = "POST";
                request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                request.Headers.Add("Ocp-Apim-Subscription-Region", location);
                request.ContentType = "application/json";

                object[] body = new object[] { new { Text = sourceText } };
                string requestBody = JsonConvert.SerializeObject(body);

                //string body = string.Format("source={0}&target={1}&text={2}", sourceLanguage, targetLanguage, sourceText);
                byte[] bytearry = Encoding.UTF8.GetBytes(requestBody);
                request.ContentLength = bytearry.Length;

                Stream st = request.GetRequestStream();
                st.Write(bytearry, 0, bytearry.Length);
                st.Close();

                // 응답 데이터 가져오기 (출력포맷)
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                string text = reader.ReadToEnd();

                stream.Close();
                response.Close();
                reader.Close();

                JArray jsonArray = JArray.Parse(text);
                JObject jObject = JObject.Parse(jsonArray[0].ToString());
                string res = jObject["translations"][0]["text"].ToString();
                return res;
            }
            catch(Exception e)
            {
                log.Error(e.Message);
            }
            return string.Empty;
        }
    }
}
