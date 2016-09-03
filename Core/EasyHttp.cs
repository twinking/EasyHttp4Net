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
        private HttpWebRequest _request;
        private HttpWebResponse _response;
        private readonly List<KeyValue> _keyValues = new List<KeyValue>();
        private readonly WebHeaderCollection _defaultHeaders = new WebHeaderCollection();
        private string _baseUrl;
        private string _url;
        private Encoding _responseEncoding = Encoding.UTF8;
        private HttpWebRequest _defaultHeaderRequest;
        private HttpWebRequest _tempRequest;
        private string _customePostData;

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




        private bool _isMultpart = false;


        private Encoding _postEncoding = Encoding.UTF8;


        public delegate HttpWebResponse InterceptorDelegate(HttpWebRequest request);


        public InterceptorDelegate RequestInterceptor;

        private readonly WebHeaderCollection _headers = new WebHeaderCollection();


        private readonly CookieContainer _cookieContainer = new CookieContainer();

        /// <summary>
        /// 获取当前网站的cookie
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> Cookies()
        {
            Dictionary<string,string> dic = new Dictionary<string, string>();
            var cookieCollection = _cookieContainer.GetCookies(new Uri(_baseUrl));
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
        public string CookieHeader()
        {
            return _cookieContainer.GetCookieHeader(new Uri(_baseUrl));
        }


        private EasyHttp()
        {
        }

        /// <summary>
        /// 添加一个参数
        /// </summary>
        /// <param name="key">参数名</param>
        /// <param name="value">参数值</param>
        /// <returns></returns>
        public EasyHttp Data(string key, string value)
        {
            KeyValue keyValue = new KeyValue(key, value);
            _keyValues.Add(keyValue);
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
            _isMultpart = true;
            KeyValue multiPartContent = new KeyValue();
            multiPartContent.Key = key;
            multiPartContent.Value = fileName;
            multiPartContent.FilePath = filePath;
            _keyValues.Add(multiPartContent);


            return this;
        }

        /// <summary>
        /// 设置超时时间
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public EasyHttp TimeOut(int timeout)
        {
            _tempRequest.Timeout = timeout;
            return this;
        }


        public EasyHttp DefaultTimeOut(int timeout)
        {
            _defaultHeaderRequest.Timeout = timeout;
            return this;
        }


        /// <summary>
        /// 添加一系列参数
        /// </summary>
        /// <param name="nameValueCollection"></param>
        /// <returns></returns>
        public EasyHttp Data(List<KeyValue> keyValues)
        {
            this._keyValues.AddRange(keyValues);
            return this;
        }

        /// <summary>
        /// 重新定义一个网络请求，这个操作将会清空以前设定的参数
        /// </summary>
        /// <param name="url">要请求的url</param>
        public EasyHttp NewRequest(string url)
        {
            if (_defaultHeaderRequest == null) _defaultHeaderRequest = WebRequest.Create(url) as  HttpWebRequest;

            _headers.Clear();
            _keyValues.Clear();
            _isMultpart = false;
            _customePostData = null;
            _keyValues.Clear();
            
            Uri uri = new Uri(url);


            string query = uri.Query;
            Console.WriteLine(query);
            //分解query参数
            if (!string.IsNullOrEmpty(query))
            {
                NameValueCollection nameValueCollection = HttpUtility.ParseQueryString(query);
                foreach (string key in nameValueCollection.Keys)
                {
                    if(key==null) _keyValues.Add(new KeyValue(nameValueCollection[key],key));
                    else _keyValues.Add(new KeyValue(key, nameValueCollection[key]));
                }
                this._url = url.Remove(url.IndexOf('?'));
            }
            else this._url = url;

           
            _baseUrl = "http://" + uri.Host;
            

            //创建temprequest

            _tempRequest = WebRequest.Create(this._url) as HttpWebRequest;

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


        public HttpWebRequest Request()
        {
            return this._request ?? _tempRequest;
        }

        public HttpWebResponse Response()
        {
            return _response;
        }




        #region 设置头信息
        /// <summary>
        /// 设置自定义头部键值对
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public EasyHttp HeaderCustome(string name, string value)
        {
            _headers.Add(name, value);
            return this;
        }

        public EasyHttp DefaultHeaderCustome(string name, string value)
        {
            _defaultHeaders.Add(name, value);
            return this;
        }


        public EasyHttp UserAgent(string userAgent)
        {
            _tempRequest.UserAgent = userAgent;
            return this;
        }


        public EasyHttp DefaultUserAgent(string userAgent)
        {
            _defaultHeaderRequest.UserAgent = userAgent;
            return this;
        }


        public EasyHttp Referer(string referer)
        {
            _tempRequest.Referer = referer;
            return this;
        }


        public EasyHttp DefaultReferer(string referer)
        {
            _defaultHeaderRequest.Referer = referer;
            return this;
        }

        public EasyHttp AcceptEncoding(string acceptEncoding)
        {
            _headers.Add("Accept-Encoding",acceptEncoding);
            return this;
        }


        public EasyHttp DefaultAcceptEncoding(string acceptEncoding)
        {
            _defaultHeaders.Add("Accept-Encoding", acceptEncoding);
            return this;
        }

        public EasyHttp AcceptLanguage(string acceptLanguage)
        {
            _headers.Add("Accept-Language", acceptLanguage);
            return this;
        }

        public EasyHttp DefaultAcceptLanguage(string acceptLanguage)
        {
            _defaultHeaders.Add("Accept-Language", acceptLanguage);
            return this;
        }

        public EasyHttp Accept(string accept)
        {
            _tempRequest.Accept = accept;
            return this;
        }

        public EasyHttp DefaultAccept(string accept)
        {
            _defaultHeaderRequest.Accept = accept;
            return this;
        }

        public EasyHttp AsMultiPart()
        {
            _isMultpart = true;
            return this;
        }


        public EasyHttp ContentType(string contentType)
        {
            _tempRequest.ContentType = contentType;
            return this;
        }

        public EasyHttp DefaultContentType(string contentType)
        {
            _defaultHeaderRequest.ContentType = contentType;
            return this;
        }


        public EasyHttp KeepAlive(bool keepAlive)
        {
            _tempRequest.KeepAlive = keepAlive;
            return this;
        }

        public EasyHttp DefaultKeepAlive(bool keepAlive)
        {
            _defaultHeaderRequest.KeepAlive = keepAlive;
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
            _cookieContainer.Add(new Uri(_baseUrl), cookie);
            return this;
        }


        public EasyHttp CookieHeader(string cookieHeader)
        {
            var cookies = cookieHeader.Split(';');
            foreach (string strCookie in cookies)
            {
                if (strCookie.Contains("="))
                {
                    var cookieKeyValue = strCookie.Split('=');
                    Cookie(cookieKeyValue[0], cookieKeyValue[1]);
                }
            }
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
            this._response = webResponse;

            return webResponse.GetResponseStream();
        }



        /// <summary>
        /// 设定post数据的编码
        /// </summary>
        /// <param name="encoding">post编码</param>
        public void PostEncoding(Encoding encoding)
        {
            this._postEncoding = encoding;
        }


        private void writeHeader()
        {


            foreach (string key in _defaultHeaders.AllKeys)
            {
                if (!WebHeaderCollection.IsRestricted(key))
                {
                    _request.Headers.Add(key, _defaultHeaders[key]);
                }
                else
                {

                    // do some thing, use HttpWebRequest propertiers to add restricted http header.
                }
            }


            foreach (string key in _headers.AllKeys)
            {
                if (!WebHeaderCollection.IsRestricted(key))
                {
                    _request.Headers.Add(key, _headers[key]);
                    if (_request.Headers.Get(key) != null)
                    {
                        _request.Headers.Set(key,_headers[key]);
                    }
                }
                else
                {

                    // do some thing, use HttpWebRequest propertiers to add restricted http header.
                }
            }
        }

        /// <summary>
        /// 根据指定方法执行请求，并返回原始Response
        /// </summary>
        /// <param name="method">http方法</param>
        /// <returns></returns>
        public HttpWebResponse Execute(Method method)
        {


          
            //get方式直接拼接url
            if (method == Method.GET)
            {
                string url = this._url;
                if (_keyValues.Count > 0)
                {
                    url = url + "?" + EasyHttpUtils.NameValuesToQueryParamString(_keyValues);
                }
                _request = WebRequest.Create(url) as HttpWebRequest;
                EasyHttpUtils.copyHttpHeader(_tempRequest,_defaultHeaderRequest, _request);
                _request.Method = "GET";
                _request.CookieContainer = _cookieContainer;
                writeHeader();
            }
            //post方式需要写入
            else if (method == Method.POST)
            {
                _request = _tempRequest;
                _request.CookieContainer = _cookieContainer;
                _request.Method = "POST";
                EasyHttpUtils.copyHttpHeader(_tempRequest, _defaultHeaderRequest, _request);

                writeHeader();
                if (_isMultpart)
                {
                    EasyHttpUtils.WriteFileToRequest(_request, _keyValues);
                }
                else
                {
                    if(string.IsNullOrEmpty(_request.ContentType))
                    _request.ContentType = "application/x-www-form-urlencoded";
                    string querystring = EasyHttpUtils.NameValuesToQueryParamString(_keyValues);
                    //如果有自定义post内容，则写入自定义post数据，否则写入form
                    if (_customePostData != null) querystring = _customePostData;
                    //写入到post
                    using (var stream = _request.GetRequestStream())
                    {
                        byte[] postData = _postEncoding.GetBytes(querystring);
                        stream.Write(postData, 0, postData.Length);
                        // Request.ContentLength = postData.Length;
                    }
                }

            }
            else if (method == Method.PUT)
            {

                string url = this._url;
                if (_keyValues.Count > 0)
                {
                    url = url + "?" + EasyHttpUtils.NameValuesToQueryParamString(_keyValues);
                }
                _request = WebRequest.Create(url) as HttpWebRequest;
                _request.CookieContainer = _cookieContainer;
                writeHeader();
                EasyHttpUtils.copyHttpHeader(_tempRequest, _defaultHeaderRequest, _request);
                _request.Method = "PUT";

            }
            else if (method == Method.DELETE)
            {
                string url = this._url;
                if (_keyValues.Count > 0)
                {
                    url = url + "?" + EasyHttpUtils.NameValuesToQueryParamString(_keyValues);
                }
                _request = WebRequest.Create(url) as HttpWebRequest;
                _request.CookieContainer = _cookieContainer;
                EasyHttpUtils.copyHttpHeader(_tempRequest, _defaultHeaderRequest, _request);
                _request.Method = "DELETE";
                writeHeader();


            }


            //Request.CookieContainer.Add(c);
            if (RequestInterceptor != null)
            {
                _response = RequestInterceptor.Invoke(_request);
            }
            else
            {

                _response = _request.GetResponse() as HttpWebResponse;
            }
            _cookieContainer.Add(_response.Cookies);

            return _response;
        }

        /// <summary>
        /// 手动设置网页编码
        /// </summary>
        /// <param name="responseEncoding"></param>
        /// <returns></returns>
        public EasyHttp ResponseEncoding(Encoding responseEncoding)
        {
            this._responseEncoding = responseEncoding;
            return this;
        }

        /// <summary>
        /// 执行GET请求，获取返回的html
        /// </summary>
        /// <returns></returns>
        public string GetForString()
        {
            return EasyHttpUtils.ReadAllAsString(ExecutForStream(Method.GET), _responseEncoding);
        }
        /// <summary>
        /// 执行Post请求，获取返回的html
        /// </summary>
        /// <returns></returns>
        public string PostForString()
        {
            return EasyHttpUtils.ReadAllAsString(ExecutForStream(Method.POST), _responseEncoding);
        }

        /// <summary>
        /// 用指定的post内容执行post请求
        /// </summary>
        /// <param name="postData">post的数据</param>
        /// <returns></returns>
        public string PostForString(string postData)
        {
            _customePostData = postData;
            return EasyHttpUtils.ReadAllAsString(ExecutForStream(Method.POST), _responseEncoding);
        }


        /// <summary>
        /// 执行Put请求，获取返回的html
        /// </summary>
        /// <returns></returns>
        public string PutForString()
        {
            return EasyHttpUtils.ReadAllAsString(ExecutForStream(Method.PUT), _responseEncoding);
        }
        /// <summary>
        /// 执行DELETE请求，获取返回的html
        /// </summary>
        /// <returns></returns>
        public string DeleteForString()
        {
            return EasyHttpUtils.ReadAllAsString(ExecutForStream(Method.DELETE), _responseEncoding);
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



        public void GetForFastRequest()
        {
          ExecuteForFastRequest(Method.GET);
        }

        public void PostForFastRequest()
        {
            ExecuteForFastRequest(Method.POST);
        }

        public void PutForFastRequest()
        {
            ExecuteForFastRequest(Method.PUT);
        }

        public void DeleteForFastRequest()
        {
            ExecuteForFastRequest(Method.DELETE);
        }

        public void ExecuteForFastRequest(Method method)
        {
            var webResponse = Execute(method);
            _response = webResponse;
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
            long total = _response.ContentLength;
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
