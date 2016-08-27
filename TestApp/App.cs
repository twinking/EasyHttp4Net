using System;
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
            
            EasyHttp http = EasyHttp.With("http://www.chenkaihua.com");
            var html = http.GetForString();
            //http.Response.Cookies
            var html2 =  http.NewRequest("http://github.chenkaihua.com/2016/08/24/c-webrequest-multpart-multi-file-upload.html")
                .GetForString();


        }

    }
}
