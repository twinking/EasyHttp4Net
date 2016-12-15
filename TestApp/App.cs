using System;
using System.Net;
using System.Text;
using EasyHttp4Net.Core;

namespace EasyHttp4Net.TestApp
{
    public class App
    {


        static void Main()
        {
         testGet();
           
        }

        static void testGet()
        {
         Cookie cookie = new Cookie();
            cookie.Name = "test";
            cookie.Value = "testvalue";
            cookie.Path = "/";
            CookieContainer cc = new CookieContainer();
            cc.Add(new Uri("http://www.chenkaihua.com"),cookie);


        }
    }
}
