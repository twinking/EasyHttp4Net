﻿using System;
using System.CodeDom;
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
    public partial class EasyHttp
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
        
        
        private EasyHttpLogLevel _logLevel = EasyHttpLogLevel.None;
        private EasyHttpLogLevel _defaultLogLevel = EasyHttpLogLevel.None;

        

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
        /// <summary>
        /// 
        /// </summary>
        public enum EasyHttpLogLevel
        {
            /// <summary>
            /// 无log
            /// </summary>
            None,
            /// <summary>
            /// write request headers and response headers
            /// </summary>
            Header,
            /// <summary>
            /// write response Body as string
            /// </summary>
            Body,
            /// <summary>
            /// write Header,Body
            /// </summary>
            All
        }




        private bool _isMultpart = false;


        private Encoding _postEncoding = Encoding.UTF8;


        public delegate HttpWebResponse InterceptorDelegate(HttpWebRequest request);


        public InterceptorDelegate RequestInterceptor;

        private readonly WebHeaderCollection _headers = new WebHeaderCollection();


        private readonly CookieContainer _cookieContainer = new CookieContainer();

        /// <summary>
        /// 以Multpart方式提交参数或文件
        /// </summary>
        /// <returns></returns>
        public EasyHttp AsMultiPart()
        {
            _isMultpart = true;
            return this;
        }





        /// <summary>
        /// set LogLell
        /// </summary>
        /// <param name="logLevel">logLevl</param>
        /// <returns></returns>
        public EasyHttp LogLevel(EasyHttpLogLevel logLevel)
        {
            _logLevel = logLevel;
            return this;
        }

 
        /// <summary>
        /// set default loglevl
        /// </summary>
        /// <param name="defaultLogLevel"> default log level</param>
        /// <returns></returns>
        public EasyHttp DefaultLogLevel(EasyHttpLogLevel defaultLogLevel)
        {
            _logLevel = defaultLogLevel;
            _defaultLogLevel = defaultLogLevel;
            return this;
        }


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
        /// get CookieContainer
        /// </summary>
        /// <returns>AllCookies</returns>
        public CookieContainer CookieContainer()
        {
            return _cookieContainer;
        }
        /// <summary>
        /// get cookies as CookieHeader by url
        /// </summary>
        /// <param name="url">url</param>
        /// <returns></returns>
        public string CookieHeaderByUrl(string url)
        {
            Uri uri = new Uri(url);
            return _cookieContainer.GetCookieHeader(uri);
        }


        /// <summary>
        /// 获取http Header中cookie的值
        /// </summary>
        /// <returns></returns>
        public string CookieHeader()
        {
            string url = string.Empty;
            if (_response == null)
            {
                url = _baseUrl;
            }
             else url = _response.ResponseUri.Scheme + "://" + _response.ResponseUri.Host;
            return CookieHeaderByUrl(url);
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
        /// 获取请求的Cookie行
        /// </summary>
        /// <returns></returns>
        public string RequestCookieHeader()
        {
            if (_request == null) return string.Empty;
            return _request.Headers["Cookie"];
        }

        public string ResponseCookieHeader()
        {
            if (_response == null) return string.Empty;
            return _response.Headers["Set-Cookie"];
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
            return NewRequest(new Uri(url));
        }

        /// <summary>
        /// 创建一个新请求,并使用之前请求获取或者手动设置的Cookie，并在请求完后保存cookie
        /// </summary>
        /// <param name="uri">url地址</param>
        /// <returns></returns>
        public EasyHttp NewRequest(Uri uri)
        {
            _url = uri.ToString();
            if (_defaultHeaderRequest == null)
            {
                _defaultHeaderRequest = WebRequest.Create(_url) as HttpWebRequest;
                _defaultHeaderRequest.ServicePoint.Expect100Continue = false;
            }
            _logLevel = _defaultLogLevel;
            _headers.Clear();
            _keyValues.Clear();
            _isMultpart = false;
            _customePostData = null;
            _keyValues.Clear();

            _baseUrl = uri.Scheme+"://"+uri.Host;

            //创建temprequest
            _request = null;
            _response = null;
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
            return With(new Uri(url));
        }

        /// <summary>
        /// 通过url创建一个全新无任何cookie的EasyHttp
        /// </summary>
        /// <param name="url">url地址</param>
        /// <returns>创建的EasyHttp</returns>
        public static EasyHttp With(Uri url)
        {
            
            EasyHttp http = new EasyHttp();
            http.NewRequest(url);
            return http;
        }




        /// <summary>
        /// 获取请求的原始<see cref="HttpWebRequest"/>对象
        /// </summary>
        /// <returns><see cref="HttpWebRequest"/></returns>
        public HttpWebRequest Request()
        {
            return this._request ?? _tempRequest;
        }
        /// <summary>
        /// 获取请求的原始<see cref="HttpWebResponse"/>对象
        /// </summary>
        /// <returns><see cref="HttpWebRequest"/></returns>
        public HttpWebResponse Response()
        {
            return _response;
        }

        





        /// <summary>
        /// 添加一个cookie
        /// </summary>
        /// <param name="name">cookie名</param>
        /// <param name="value">cook值</param>
        /// <returns></returns>
        public EasyHttp Cookie(string name, string value)
        {
            try
            {
                System.Net.Cookie cookie = new Cookie();
                cookie.Name = name;
                cookie.Value = value;
                _cookieContainer.Add(new Uri(_baseUrl), cookie);
            }
            catch (Exception)
            {
                
            }
            return this;
        }

        /// <summary>
        /// 设置请求的Cookie，例如:<c>a=avlue;c=cvalue</c>
        /// </summary>
        /// <param name="cookieHeader">Cookie,例如:<c>a=avlue;c=cvalue</c></param>
        /// <returns></returns>
        public EasyHttp CookieHeader(string cookieHeader)
        {
            if (cookieHeader == null) return this;
            var substr = cookieHeader.Split(';');
            foreach (string str in substr)
            {
                var cookieLines = str.Split(',');
                foreach (string cookieLine in cookieLines)
                {
                    if (cookieLine.Contains("="))
                    {
                        var cookieKeyValue = cookieLine.Split('=');
                        var key = cookieKeyValue[0].Trim();
                        var value = cookieKeyValue[1].Trim();
                        var toLowerKey = key.ToLower();
                        if (toLowerKey != "expires" &&
                            toLowerKey != "path" && toLowerKey != "domain" && toLowerKey != "max-age"
                            && toLowerKey != "HttpOnly")
                        {
                            Cookie(key,value);
                        }
                    }
                }
            }

            return this;
        }


     

        /// <summary>
        /// 碰到302等状态时，是否自动转入新网址
        /// </summary>
        /// <param name="allowAutoRedirect"></param>
        /// <returns></returns>
        public EasyHttp AllowAutoRedirect(bool allowAutoRedirect)
        {
            _tempRequest.AllowAutoRedirect = allowAutoRedirect;
            return this;
        }

        public EasyHttp DefaultAllowAutoRedirect(bool allowAutoRedirect)
        {
            _defaultHeaderRequest.AllowAutoRedirect = allowAutoRedirect;
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


        private void WriteHeader()
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

        private void UrlToQuery(string url)
        {
            Uri uri = new Uri(url);
            string query = uri.Query;
            //分解query参数
            if (!string.IsNullOrEmpty(query))
            {
                NameValueCollection nameValueCollection = HttpUtility.ParseQueryString(query);
                foreach (string key in nameValueCollection.Keys)
                {
                    if (key == null) _keyValues.Add(new KeyValue(nameValueCollection[key], key));
                    else _keyValues.Add(new KeyValue(key, nameValueCollection[key]));
                }
                this._url = url.Remove(url.IndexOf('?'));
            }
            else this._url = uri.ToString();
            _baseUrl = uri.Scheme + uri.Host;

        }


        /// <summary>
        /// 根据指定方法执行请求，并返回原始Response
        /// </summary>
        /// <param name="method">http方法</param>
        /// <returns></returns>
        public HttpWebResponse Execute(Method method)
        {


            string url = string.Empty;
            //get方式直接拼接url
            if (method == Method.GET)
            {
                UrlToQuery(_url);
                url = this._url;
                if (_keyValues.Count > 0)
                {
                    //分解参数
                    url = url + "?" + EasyHttpUtils.NameValuesToQueryParamString(_keyValues);
                }
                _request = WebRequest.Create(url) as HttpWebRequest;
                
                EasyHttpUtils.CopyHttpHeader(_tempRequest,_defaultHeaderRequest, _request);
                
                _request.Method = "GET";
                _request.CookieContainer = _cookieContainer;
                WriteHeader();
            }
            //post方式需要写入
            else if (method == Method.POST)
            {
                url = _url;
                _request = _tempRequest;
                
                _request.CookieContainer = _cookieContainer;
                _request.Method = "POST";
                EasyHttpUtils.CopyHttpHeader(_tempRequest, _defaultHeaderRequest, _request);
                WriteHeader();
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
                UrlToQuery(_url);
                 url = this._url;
                if (_keyValues.Count > 0)
                {
                    url = url + "?" + EasyHttpUtils.NameValuesToQueryParamString(_keyValues);
                }
                _request = WebRequest.Create(url) as HttpWebRequest;
                _request.CookieContainer = _cookieContainer;
                
                WriteHeader();
                EasyHttpUtils.CopyHttpHeader(_tempRequest, _defaultHeaderRequest, _request);
                _request.Method = "PUT";

            }
            else if (method == Method.DELETE)
            {
                UrlToQuery(_url);
                 url = this._url;
                if (_keyValues.Count > 0)
                {
                    url = url + "?" + EasyHttpUtils.NameValuesToQueryParamString(_keyValues);
                }
                _request = WebRequest.Create(url) as HttpWebRequest;
                
                _request.CookieContainer = _cookieContainer;
                EasyHttpUtils.CopyHttpHeader(_tempRequest, _defaultHeaderRequest, _request);
                _request.Method = "DELETE";
                WriteHeader();


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

            if (_logLevel!= EasyHttpLogLevel.None)
            {
                try
                {
                    
                LogRequet(url,method);
                    LogRespose(url,method);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                }
            }

            

            return _response;
        }





        private void LogRequestParams()
        {
            if (_keyValues.Count > 0)
            {
                foreach (KeyValue keyValue in _keyValues)
                {
                    Console.WriteLine($"\t{keyValue.Key}={keyValue.Value}");
                }
            }
        }


        


        private void LogRequet(string url,Method method)
        {
            if(_logLevel== EasyHttpLogLevel.None) return;
            Console.WriteLine($">>> {_request.Method}->{_request.RequestUri}");
            if (_logLevel == EasyHttpLogLevel.Header || _logLevel==EasyHttpLogLevel.All)
            {
                Console.WriteLine("Request_Headers:");
                var webHeaderCollection = _request.Headers;

                foreach (string key in webHeaderCollection.Keys)
                {
                    Console.WriteLine($"\t{key}:{webHeaderCollection[key]}");
                }
            }

            if (_logLevel == EasyHttpLogLevel.Body || _logLevel==EasyHttpLogLevel.All)
            {
                if (method == Method.POST)
                {
                    Console.WriteLine($"Request_Body:");

                    if (_customePostData != null)
                    {
                        Console.WriteLine("\t" + _customePostData);
                    }
                    else
                    {
                        LogRequestParams();
                    }
                }
                else
                {
                    Console.WriteLine($"Request_Params:");
                    LogRequestParams();
                }


            }
         


        }

        private void LogRespose(string url, Method method)
        {
            if (_logLevel == EasyHttpLogLevel.None) return;
            if (_logLevel == EasyHttpLogLevel.Header||_logLevel==EasyHttpLogLevel.All) {

                Console.WriteLine($"<<< {_response.Method}->{_response.ResponseUri}->{_response.StatusCode}");
                Console.WriteLine("Response_Headers:");
                var webHeaderCollection = _response.Headers;

            foreach (string key in webHeaderCollection.Keys)
            {
                Console.WriteLine($"\t{key}:{webHeaderCollection[key]}");
            }
            }

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
            var stream = ExecutForStream(Method.GET);


            string str = EasyHttpUtils.ReadAllAsString(stream, _responseEncoding);

          //  var executForStream = ExecutForStream(Method.GET);
            

            LogHtml(str);
            return str;
        }


        private bool IsResponseGzipCompress()
        {
            if (_response!= null && _response.ContentEncoding != null &&
                _response.ContentEncoding.Equals("gzip", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

                return false;
        }


        /// <summary>
        /// 执行Post请求，获取返回的html
        /// </summary>
        /// <returns></returns>
        public string PostForString()
        {
            var str = EasyHttpUtils.ReadAllAsString(ExecutForStream(Method.POST), _responseEncoding);
            LogHtml(str);
            return str;
        }

        /// <summary>
        /// 用指定的post内容执行post请求
        /// </summary>
        /// <param name="postData">post的数据</param>
        /// <returns></returns>
        public string PostForString(string postData)
        {
            _customePostData = postData;
            var str = EasyHttpUtils.ReadAllAsString(ExecutForStream(Method.POST), _responseEncoding);
            LogHtml(str);
            return str;
        }


        /// <summary>
        /// 执行Put请求，获取返回的html
        /// </summary>
        /// <returns></returns>
        public string PutForString()
        {
            var str = EasyHttpUtils.ReadAllAsString(ExecutForStream(Method.PUT), _responseEncoding);
            LogHtml(str);
            return str;
        }


        private void LogHtml(string html)
        {
            if (_logLevel == EasyHttpLogLevel.Body || _logLevel==EasyHttpLogLevel.All)
            {
                Console.WriteLine("HTML:");
                Console.WriteLine(html);
            }
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
        public void GetForFile(string filePath)
        {
             ExecuteForFile(filePath, Method.GET);
        }

        /// <summary>
        /// 执行Post请求，并把返回内容作为文件保存到指定路径
        /// </summary>
        /// <param name="filePath">文件路径.包括文件名</param>
        /// <returns></returns>
        public void PostForFile(string filePath)
        {
             ExecuteForFile(filePath, Method.POST);
        }

        /// <summary>
        /// 执行Put请求，并把返回内容作为文件保存到指定路径
        /// </summary>
        /// <param name="filePath">文件路径.包括文件名</param>
        /// <returns></returns>
        public void PutForFile(string filePath)
        {
             ExecuteForFile(filePath, Method.PUT);
        }

        /// <summary>
        /// 执行Delete请求，并把返回内容作为文件保存到指定路径
        /// </summary>
        /// <param name="filePath">文件路径.包括文件名</param>
        /// <returns></returns>
        public void DeleteForFile(string filePath)
        {
             ExecuteForFile(filePath, Method.DELETE);
        }


        /// <summary>
        /// 以Get方式快速请求，舍弃返回内容
        /// </summary>
        public void GetForFastRequest()
        {
          ExecuteForFastRequest(Method.GET);
        }
        /// <summary>
        /// 以Post方法快速请求，舍弃返回内容
        /// </summary>
        public void PostForFastRequest()
        {
            ExecuteForFastRequest(Method.POST);
        }
        /// <summary>
        /// 以PUT方式快速请求，舍弃返回内容
        /// </summary>
        public void PutForFastRequest()
        {
            ExecuteForFastRequest(Method.PUT);
        }
        /// <summary>
        /// 以Delete方式快速请求，舍弃返回内容
        /// </summary>
        public void DeleteForFastRequest()
        {
            ExecuteForFastRequest(Method.DELETE);
        }
        /// <summary>
        ///以指定的Http Methond 执行快速请求，舍弃返回内容
        /// </summary>
        /// <param name="method">如<code>GET,POST,PUT,DELETE</code>等</param>
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
        public long ExecuteForFile(string filePath, Method method)
        {
            var stream = ExecutForStream(method);
            long total = _response.ContentLength;
            return EasyHttpUtils.ReadAllAsFile(stream, total, filePath);
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
