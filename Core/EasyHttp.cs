using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace EasyHttp4Net.Core
{
    /// <summary>
    /// 框架的核心类，自动处理cookie，并封装了很多简单的api
    /// </summary>
    public class EasyHttp
    {
        public HttpWebRequest Request;
        public HttpWebResponse Response;
        private List<KeyValue> keyValues = new List<KeyValue>();
        private string baseUrl;
        private Encoding responseEncoding = Encoding.UTF8;

        private HttpWebRequest tempRequest;
        private string customePostData;

        /// <summary>
        /// 代表HTTP的方法
        /// </summary>
        public enum Method
        {
            /// <summary>
            /// GET方法
            /// </summary>
            GET,
            /// <summary>
            /// post方法
            /// </summary>
            POST,
            /// <summary>
            /// PUT方法
            /// </summary>
            PUT,
            /// <summary>
            /// Delete方法
            /// </summary>
            DELETE
        }




        private bool isMultpart = false;


        private Encoding postEncoding = Encoding.UTF8;


        public delegate HttpWebResponse InterceptorDelegate(HttpWebRequest request);


        public InterceptorDelegate requestInterceptor;

        public WebHeaderCollection Headers = new WebHeaderCollection();


        public CookieContainer cookieContainer = new CookieContainer();

        /// <summary>
        /// 获取当前网站的cookie
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> cookies()
        {
            Dictionary<string,string> dic = new Dictionary<string, string>();
            var cookieCollection = cookieContainer.GetCookies(new Uri(baseUrl));
            foreach (Cookie c in cookieCollection)
            {
                if(!dic.ContainsKey(c.Name))
                dic.Add(c.Name,c.Value);
                else
                {
                    dic[c.Name] = c.Value;
                }
            }
            return dic;
        }

        /// <summary>
        /// 获取http Header中cookie的值
        /// </summary>
        /// <returns></returns>
        public string cookieHeader()
        {
            return cookieContainer.GetCookieHeader(new Uri(baseUrl));
        }


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
        /// 设置超时时间
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public EasyHttp TimeOut(int timeout)
        {
            tempRequest.Timeout = timeout;
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
            customePostData = null;
            keyValues.Clear();
            //分解query参数
            if (url.Contains("?") && url.Contains("="))
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

            tempRequest = WebRequest.Create(baseUrl) as HttpWebRequest;

            return this;
        }


        /// <summary>
        /// 通过url开启一个EasyHttp
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
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





        /// <summary>
        /// 添加一个cookie
        /// </summary>
        /// <param name="name">cookie名</param>
        /// <param name="value">cook值</param>
        /// <returns></returns>
        public EasyHttp Cookie(string name, string value)
        {
            System.Net.Cookie cookie = new Cookie();
            cookie.Name = name;
            cookie.Value = value;
            cookieContainer.Add(new Uri(baseUrl), cookie);
            return this;
        }


        /// <summary>
        /// 根据指定的方法，获取返回内容的stream
        /// </summary>
        /// <param name="method">http方法</param>
        /// <returns></returns>
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
        /// 根据指定方法执行请求，并返回原始Response
        /// </summary>
        /// <param name="method">http方法</param>
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
                Request = WebRequest.Create(url) as HttpWebRequest;
                EasyHttpUtils.copyHttpHeader(tempRequest, Request);
                Request.Method = "GET";

            }
            //post方式需要写入
            else if (method == Method.POST)
            {
                Request = tempRequest;
                Request.CookieContainer = cookieContainer;
                Request.Method = "POST";
                if (isMultpart)
                {
                    EasyHttpUtils.WriteFileToRequest(Request, keyValues);
                }
                else
                {
                    if(string.IsNullOrEmpty(Request.ContentType))
                    Request.ContentType = "application/x-www-form-urlencoded";
                    string querystring = EasyHttpUtils.NameValuesToQueryParamString(keyValues);
                    //如果有自定义post内容，则写入自定义post数据，否则写入form
                    if (customePostData != null) querystring = customePostData;
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
                Request = WebRequest.Create(url) as HttpWebRequest;
                Request.CookieContainer = cookieContainer;
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
                Request = WebRequest.Create(url) as HttpWebRequest;
                Request.CookieContainer = cookieContainer;
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
            cookieContainer.Add(Response.Cookies);

            return Response;
        }

        /// <summary>
        /// 手动设置网页编码
        /// </summary>
        /// <param name="responseEncoding"></param>
        /// <returns></returns>
        public EasyHttp ResponseEncoding(Encoding responseEncoding)
        {
            this.responseEncoding = responseEncoding;
            return this;
        }

        /// <summary>
        /// 执行GET请求，获取返回的html
        /// </summary>
        /// <returns></returns>
        public string GetForString()
        {
            return EasyHttpUtils.ReadAllAsString(ExecutForStream(Method.GET), responseEncoding);
        }
        /// <summary>
        /// 执行Post请求，获取返回的html
        /// </summary>
        /// <returns></returns>
        public string PostForString()
        {
            return EasyHttpUtils.ReadAllAsString(ExecutForStream(Method.POST), responseEncoding);
        }

        /// <summary>
        /// 用指定的post内容执行post请求
        /// </summary>
        /// <param name="postData">post的数据</param>
        /// <returns></returns>
        public string PostForString(string postData)
        {
            customePostData = postData;
            return EasyHttpUtils.ReadAllAsString(ExecutForStream(Method.POST), responseEncoding);
        }


        /// <summary>
        /// 执行Put请求，获取返回的html
        /// </summary>
        /// <returns></returns>
        public string PutForString()
        {
            return EasyHttpUtils.ReadAllAsString(ExecutForStream(Method.PUT), responseEncoding);
        }
        /// <summary>
        /// 执行DELETE请求，获取返回的html
        /// </summary>
        /// <returns></returns>
        public string DeleteForString()
        {
            return EasyHttpUtils.ReadAllAsString(ExecutForStream(Method.DELETE), responseEncoding);
        }

        /// <summary>
        /// 执行Get请求，并把返回内容作为文件保存到指定路径
        /// </summary>
        /// <param name="filePath">文件路径.包括文件名</param>
        /// <returns></returns>
        public bool GetForFile(string filePath)
        {
            return ExecuteForFile(filePath, Method.GET);
        }

        /// <summary>
        /// 执行Post请求，并把返回内容作为文件保存到指定路径
        /// </summary>
        /// <param name="filePath">文件路径.包括文件名</param>
        /// <returns></returns>
        public bool PostForFile(string filePath)
        {
            return ExecuteForFile(filePath, Method.POST);
        }

        /// <summary>
        /// 执行Put请求，并把返回内容作为文件保存到指定路径
        /// </summary>
        /// <param name="filePath">文件路径.包括文件名</param>
        /// <returns></returns>
        public bool PutForFile(string filePath)
        {
            return ExecuteForFile(filePath, Method.PUT);
        }

        /// <summary>
        /// 执行Delete请求，并把返回内容作为文件保存到指定路径
        /// </summary>
        /// <param name="filePath">文件路径.包括文件名</param>
        /// <returns></returns>
        public bool DeleteForFile(string filePath)
        {
            return ExecuteForFile(filePath, Method.DELETE);
        }



        /// <summary>
        /// 执行指定方法的请求，将返回内容保存在指定路径的文件中
        /// </summary>
        /// <param name="filePath">包含文件名的路径</param>
        /// <param name="method">http Method</param>
        /// <returns></returns>
        public bool ExecuteForFile(string filePath, Method method)
        {
            var stream = ExecutForStream(method);
            long total = Response.ContentLength;
            return EasyHttpUtils.ReadAllAsFile(stream, total, filePath) == total;
        }

        /// <summary>
        /// 根据指定的方法执行请求，并把返回内容序列化为Image对象
        /// </summary>
        /// <param name="method">指定方法，GET,POST,PUT,DELETE</param>
        /// <returns></returns>
        public Image ExecuteForImage(Method method)
        {

            Stream stream = ExecutForStream(method);
            return Image.FromStream(stream);
        }
        /// <summary>
        /// 执行Get方法，并把返回内容序列化为Image对象
        /// </summary>
        /// <returns></returns>
        public Image GetForImage()
        {
            return ExecuteForImage(Method.GET);
        }

        /// <summary>
        /// 执行Post方法，并把返回内容序列化为Image对象
        /// </summary>
        /// <returns></returns>
        public Image PostForImage()
        {
            return ExecuteForImage(Method.POST);
        }
        /// <summary>
        /// 执行Put方法，并把返回内容序列化为Image对象
        /// </summary>
        /// <returns></returns>
        public Image PutForImage()
        {
            return ExecuteForImage(Method.PUT);
        }

        /// <summary>
        /// 执行Delete方法，并把返回内容序列化为Image对象
        /// </summary>
        /// <returns></returns>
        public Image DeleteForImage()
        {
            return ExecuteForImage(Method.DELETE);
        }



    }
}
