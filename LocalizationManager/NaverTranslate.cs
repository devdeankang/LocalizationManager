using log4net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LocalizationManager
{
    public class NaverTranslate : ITranslate
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(NaverTranslate));

        public string Translate(string sourceLang, string targetLang, string sourceText)
        {
            string sUrl = "https://openapi.naver.com/v1/papago/n2mt";
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(sUrl);

                // 헤더 추가하기 (파파고 NMT API 가이드에서 -h 부분이 헤더이다)
                request.Headers.Add("X-Naver-Client-Id", "");   //private
                request.Headers.Add("X-Naver-Client-Secret", "");   //private
                request.Method = "POST";


                string body = string.Format("source={0}&target={1}&text={2}", sourceLang, targetLang, sourceText);
                byte[] bytearry = Encoding.UTF8.GetBytes(body);

                request.ContentType = "application/x-www-form-urlencoded";

                // 요청 데이터 길이
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

                JObject jObject = JObject.Parse(text);
                //Console.WriteLine(jObject["message"]["result"]["translatedText"].ToString()); // 결과 출력

                string res = jObject["message"]["result"]["translatedText"].ToString();
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
