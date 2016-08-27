using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EasyHttp4Net.Core
{
  public  class EasyHttpUtils
    {
        public static string NameValuesToQueryParamString(NameValueCollection nameValueCollection)
        {
            StringBuilder builder = new StringBuilder();

            //string nameValue = nameValueCollection["s"];

            if (nameValueCollection == null || nameValueCollection.Count == 0)
            {
                return string.Empty;
            }

            foreach (string key in nameValueCollection.Keys)
            {
                var value = nameValueCollection[key];
                builder.Append(key).Append('=').Append(nameValueCollection[key]).Append("&");
            }
            builder.Remove(builder.Length - 1, 1);
            return builder.ToString();
        }


        public static void copyHttpHeader(HttpWebRequest fromRequest, HttpWebRequest toRequest)
        {
            //设置头部信息
            toRequest.Accept = fromRequest.Accept;
            toRequest.ContentType = fromRequest.ContentType;
            toRequest.Referer = fromRequest.Referer;
            toRequest.UserAgent = fromRequest.UserAgent;
            toRequest.AllowAutoRedirect = fromRequest.AllowAutoRedirect;
            toRequest.ContentType = fromRequest.ContentType;
            toRequest.AllowReadStreamBuffering = fromRequest.AllowReadStreamBuffering;
            toRequest.AutomaticDecompression = fromRequest.AutomaticDecompression;
            toRequest.ClientCertificates = fromRequest.ClientCertificates;
            toRequest.Connection = fromRequest.Connection;
            toRequest.AllowWriteStreamBuffering = fromRequest.AllowWriteStreamBuffering;
            toRequest.ContinueDelegate = fromRequest.ContinueDelegate;
            toRequest.ContinueTimeout = fromRequest.ContinueTimeout;
            toRequest.Credentials = fromRequest.Credentials;
            toRequest.Date = fromRequest.Date;
            toRequest.UseDefaultCredentials = fromRequest.UseDefaultCredentials;
            toRequest.Expect = fromRequest.Expect;
            toRequest.Host = fromRequest.Host;
            toRequest.IfModifiedSince = fromRequest.IfModifiedSince;
            toRequest.TransferEncoding = fromRequest.TransferEncoding;
            toRequest.Timeout = fromRequest.Timeout;
        }


        public static string NameValuesToQueryParamString(List<KeyValue> nameValueCollection)
        {
            StringBuilder builder = new StringBuilder();

            //string nameValue = nameValueCollection["s"];

            if (nameValueCollection == null || nameValueCollection.Count == 0)
            {
                return string.Empty;
            }

            foreach (KeyValue keyValue in nameValueCollection)
            {
                builder.Append(keyValue.Key).Append('=').Append(keyValue.Value).Append('&');
            }
            if (builder.Length > 0)

                builder.Remove(builder.Length - 1, 1);
            return builder.ToString();
        }

        public static string ReadAllAsString(Stream stream, Encoding encoding)
        {
            string html = string.Empty;
            using (var responseStream = new StreamReader(stream, encoding))
            {
                html = responseStream.ReadToEnd();
            }
            return html;
        }

        public static long ReadAllAsFile(Stream stream, long length, string filePath)
        {
            long currentTotal = 0;
            byte[] buffer = null;
            //判断文件大小，如果大于1m的，则buffer大小为10k，否则为1k
            if (length > 1 * 1024)
            {
                buffer = new byte[10 * 1024];
            }
            else
            {
                buffer = new byte[1024];
            }

            using (BinaryReader reader = new BinaryReader(stream))
            {
                using (FileStream lxFS = new FileStream(filePath, FileMode.Create))
                {
                    int size = -1;
                    while ((size = reader.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        lxFS.Write(buffer, 0, size);
                        currentTotal += size;
                    }
                }
            }
            return currentTotal;
        }

        public static void WriteFileToRequest(HttpWebRequest request, List<KeyValue> nvc)
        {
            //   log.Debug(string.Format("Uploading {0} to {1}", file, url));
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest wr = request;
            wr.ContentType = "multipart/form-data; boundary=" + boundary;
            wr.Method = "POST";
            wr.KeepAlive = true;
            wr.Credentials = System.Net.CredentialCache.DefaultCredentials;



            using (var rs = wr.GetRequestStream())
            {
                // 普通参数模板
                string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
                //带文件的参数模板
                string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
                foreach (KeyValue keyValue in nvc)
                {
                    //如果是普通参数
                    if (keyValue.FilePath == null)
                    {
                        rs.Write(boundarybytes, 0, boundarybytes.Length);
                        string formitem = string.Format(formdataTemplate, keyValue.Key, keyValue.Value);
                        byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                        rs.Write(formitembytes, 0, formitembytes.Length);
                    }
                    //如果是文件参数,则上传文件
                    else
                    {
                        rs.Write(boundarybytes, 0, boundarybytes.Length);

                        string header = string.Format(headerTemplate, keyValue.Key, keyValue.FilePath, keyValue.ContentType);
                        byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
                        rs.Write(headerbytes, 0, headerbytes.Length);

                        using (var fileStream = new FileStream(keyValue.FilePath, FileMode.Open, FileAccess.Read))
                        {
                            byte[] buffer = new byte[4096];
                            int bytesRead = 0;
                            long total = 0;
                            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                            {

                                rs.Write(buffer, 0, bytesRead);
                                total += bytesRead;
                            }
                        }
                    }

                }

                byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
                rs.Write(trailer, 0, trailer.Length);

            }

        }
    }
}
