using System;
using System.Net;
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

            var html = EasyHttp.With("http://chenkaihua.com").GetForString();
            Console.WriteLine(html);

        }

    }
}
