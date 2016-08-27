using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace EasyHttp4Net.Core
{
    public class EasyHttp
    {
        public HttpWebRequest Request;
        public HttpWebResponse Response;
        private List<KeyValue> keyValues = new List<KeyValue>();
        private string baseUrl;
        private Encoding responseEncoding = Encoding.UTF8;

        private HttpWebRequest tempRequest;


        public enum Method
        {
            GET, POST, PUT, DELETE
        }

        private bool isMultpart = false;

        private Encoding ResponseContentEncoding = Encoding.UTF8;

        private Encoding postEncoding = Encoding.UTF8;


        public delegate HttpWebResponse InterceptorDelegate(HttpWebRequest request);


        public InterceptorDelegate requestInterceptor;

        public WebHeaderCollection Headers = new WebHeaderCollection();


        public CookieContainer cookieContainer = new CookieContainer();





        private EasyHttp() { }

        /// <summary>
        /// 添加一个参数
        /// </summary>
        /// <param name="key">参数名</param>
        /// <param name="value">参数值</param>
        /// <returns></returns>
        public EasyHttp Data(string key, string value)
        {
            KeyValue keyValue = new KeyValue(key, value);
            keyValues.Add(keyValue);
            return this;
        }


        /// <summary>
        /// 添加一个multipart内容
        /// </summary>
        /// <param name="key">参数名</param>
        /// <param name="fileName">文件名</param>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public EasyHttp Data(string key, string fileName, string filePath)
        {
            isMultpart = true;
            KeyValue multiPartContent = new KeyValue();
            multiPartContent.Key = key;
            multiPartContent.Value = fileName;
            multiPartContent.FilePath = filePath;
            keyValues.Add(multiPartContent);


            return this;
        }

        /// <summary>
        /// 添加一系列参数
        /// </summary>
        /// <param name="nameValueCollection"></param>
        /// <returns></returns>
        public EasyHttp Data(List<KeyValue> keyValues)
        {
            this.keyValues.AddRange(keyValues);
            return this;
        }

        /// <summary>
        /// 重新定义一个网络请求，这个操作将会清空以前设定的参数
        /// </summary>
        /// <param name="url">要请求的url</param>
        public EasyHttp NewRequest(string url)
        {
            Headers.Clear();
            keyValues.Clear();
            isMultpart = false;
            keyValues.Clear();
            //分解query参数
            if (url.Contains('?') && url.Contains('='))
            {
                baseUrl = url.Substring(0, url.IndexOf('?'));
                string paras = url.Remove(0, url.IndexOf('?') + 1);
                NameValueCollection nameValueCollection = HttpUtility.ParseQueryString(paras);
                foreach (string key in nameValueCollection.Keys)
                {
                    keyValues.Add(new KeyValue(key, nameValueCollection[key]));
                }
            }
            else baseUrl = url;

            //创建temprequest

            tempRequest = WebRequest.CreateHttp(baseUrl);

            return this;
        }



        public static EasyHttp With(string url)
        {
            EasyHttp http = new EasyHttp();
            http.NewRequest(url);
            return http;
        }




        #region 设置头信息

        public EasyHttp Header(string name, string value)
        {
            Headers.Add(name, value);
            return this;
        }

        public EasyHttp UserAgent(string userAgent)
        {
            tempRequest.UserAgent = userAgent;
            return this;
        }


        public EasyHttp Referer(string referer)
        {
            tempRequest.Referer = referer;
            return this;
        }

        public EasyHttp Accept(string accept)
        {
            tempRequest.Accept = accept;
            return this;
        }

        public EasyHttp AsMultiPart()
        {
            isMultpart = true;
            return this;
        }


        public EasyHttp ContentType(string contentType)
        {
            tempRequest.ContentType = contentType;
            return this;
        }

        #endregion






        public EasyHttp Cookie(string name, string value)
        {
            System.Net.Cookie cookie = new Cookie();
            cookie.Name = name;
            cookie.Value = value;
            cookieContainer.Add(new Uri(baseUrl), cookie);
            return this;
        }



        /// <summary>
        /// 执行post方法，得到网页返回的stream
        /// </summary>
        /// <returns>网页返回的stream</returns>
        public Stream ExecutForStream(Method method)
        {

            HttpWebResponse webResponse = Execute(method);
            this.Response = webResponse;

            return Response.GetResponseStream();
        }



        /// <summary>
        /// 设定post数据的编码
        /// </summary>
        /// <param name="encoding">post编码</param>
        public void PostEncoding(Encoding encoding)
        {
            this.postEncoding = encoding;
        }


        /// <summary>
        /// 执行请求，请获取响应
        /// </summary>
        /// <returns></returns>
        public HttpWebResponse Execute(Method method)
        {


            foreach (string key in Headers.AllKeys)
            {
                if (!WebHeaderCollection.IsRestricted(key))
                {
                    Request.Headers.Add(key, Headers[key]);
                }
                else
                {

                    // do some thing, use HttpWebRequest propertiers to add restricted http header.
                }
            }
            //get方式直接拼接url
            if (method == Method.GET)
            {
                string url = baseUrl;
                if (keyValues.Count > 0)
                {
                    url = url + "?" + EasyHttpUtils.NameValuesToQueryParamString(keyValues);
                }
                Request = WebRequest.CreateHttp(url);
                EasyHttpUtils.copyHttpHeader(tempRequest, Request);
                Request.Method = "GET";

            }
            //post方式需要写入
            else if (method == Method.POST)
            {
                Request = tempRequest;
                Request.Method = "POST";
                if (isMultpart)
                {
                    EasyHttpUtils.WriteFileToRequest(Request, keyValues);
                }
                else
                {
                    Request.ContentType = "application/x-www-form-urlencoded";
                    string querystring = EasyHttpUtils.NameValuesToQueryParamString(keyValues);
                    //写入到post
                    using (var stream = Request.GetRequestStream())
                    {
                        byte[] postData = postEncoding.GetBytes(querystring);
                        stream.Write(postData, 0, postData.Length);
                        // Request.ContentLength = postData.Length;
                    }
                }

            }
            else if (method == Method.PUT)
            {
                string url = baseUrl;
                if (keyValues.Count > 0)
                {
                    url = url + "?" + EasyHttpUtils.NameValuesToQueryParamString(keyValues);
                }
                Request = WebRequest.CreateHttp(url);
                EasyHttpUtils.copyHttpHeader(tempRequest, Request);
                Request.Method = "PUT";

            }
            else if (method == Method.DELETE)
            {
                string url = baseUrl;
                if (keyValues.Count > 0)
                {
                    url = url + "?" + EasyHttpUtils.NameValuesToQueryParamString(keyValues);
                }
                Request = WebRequest.CreateHttp(url);
                EasyHttpUtils.copyHttpHeader(tempRequest, Request);
                Request.Method = "DELETE";

            }


            //Request.CookieContainer.Add(c);
            if (requestInterceptor != null)
            {
                Response = requestInterceptor.Invoke(Request);
            }
            else
            {

                Response = Request.GetResponse() as HttpWebResponse;
            }
            return Response;
        }



        public string GetForString()
        {
            return EasyHttpUtils.ReadAllAsString(ExecutForStream(Method.GET), responseEncoding);
        }

        public string PostForString()
        {
            return EasyHttpUtils.ReadAllAsString(ExecutForStream(Method.POST), responseEncoding);
        }


        public string PutForString()
        {
            return EasyHttpUtils.ReadAllAsString(ExecutForStream(Method.PUT), responseEncoding);
        }

        public string DeleteForString()
        {
            return EasyHttpUtils.ReadAllAsString(ExecutForStream(Method.DELETE), responseEncoding);
        }


        public bool GetForFile(string filePath)
        {
            return ExecuteForFile(filePath, Method.GET);
        }

        public bool PostForFile(string filePath)
        {
            return ExecuteForFile(filePath, Method.POST);
        }


        public bool PutForFile(string filePath)
        {
            return ExecuteForFile(filePath, Method.PUT);
        }

        public bool DeleteForFile(string filePath)
        {
            return ExecuteForFile(filePath, Method.DELETE);
        }



        /// <summary>
        /// 执行请求，请把结果保存在文件中
        /// </summary>
        /// <param name="filePath">保存文件的路径，相对路径或者绝对路径</param>
        /// <returns></returns>
        public bool ExecuteForFile(string filePath, Method method)
        {
            var stream = ExecutForStream(method);
            long total = Response.ContentLength;
            return EasyHttpUtils.ReadAllAsFile(stream, total, filePath) == total;
        }

        /// <summary>
        /// 执行请求，并获取Image对象
        /// </summary>
        /// <returns></returns>
        public Image ExecuteForImage(Method method)
        {

            Stream stream = ExecutForStream(method);
            return Image.FromStream(stream);
        }

        public Image GetForImage()
        {
            return ExecuteForImage(Method.GET);
        }


        public Image PostForImage()
        {
            return ExecuteForImage(Method.POST);
        }

        public Image PutForImage()
        {
            return ExecuteForImage(Method.PUT);
        }


        public Image DeleteForImage()
        {
            return ExecuteForImage(Method.DELETE);
        }



    }
}
