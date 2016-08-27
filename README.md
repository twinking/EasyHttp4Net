# EasyHttp4Net
.net平台下基于webrequest的简易Http请求框架

* 基于webrequest，封装繁琐的请求，一条语句即可进行一个请求
* 支持GET/POST/PUT/DELETE方法
* 自动保存cookie，无需关心cookie的存储
* 支持多文件上传
* 支持下载文件
* 超简单api

## 使用

### 安装

项目已经上传到nuget,一键安装：

``` bash
PM> Install-Package EasyHttp4Net.Core
```

### get获取网页

```c#
var html = EasyHttp.With("http://www.chenkaihua.com").Data("key","value")
                .GetForString();
```

### post表单

``` c#
 var html = EasyHttp.With("http://www.chenkaihua.com")
                .Data("key","value")
                .PostForString();
```
### 上传文件

```c#
 var html =  EasyHttp.With("http://www.chenkaihua.com")
                .Data("file","test.png","mytestfile.png")
                PostForString();
```

### 下载文件

``` c#
 EasyHttp.With("http://www.chenkaihua.com")
                .GetForFile("index.html");
```

### 下载图片

``` c#
 Image image =  EasyHttp.With("http://7xivpo.com1.z0.glb.clouddn.com/gradle-greendao-resutl.png")
                .GetForImage();
      image.Save("image.png",ImageFormat.Png);
```

### 设置cookie

```c#
var html =  EasyHttp.With("http://www.baidu.com")
                .Cookie("sessionId","cookieValue")
                .GetForString();
```

### 自动保存cookie

``` c#
EasyHttp http = EasyHttp.With("http://www.chenkaihua.com");
            var html = http.GetForString();
            //http.Response.Cookies
            var html2 =  http.NewRequest("http://github.chenkaihua.com/2016/08/24/c-webrequest-multpart-multi-file-upload.html")
                .GetForString();

```

## About

blog: <http://www.chenkaihua.com>
email: admin@chenkaihua.com