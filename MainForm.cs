using CefSharp.DevTools.IO;
using CefSharp.WinForms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace sso_2
{
    public partial class MainForm : Form
    {
        private ChromiumWebBrowser webBrowser;
        private const string BaseUrl = "https://cj.4dbim.vip/sso/api";
        private const string RedirectUri = "http://localhost:12345/";
        private const string AuthorizationEndpoint = "/v1/connect/authorize";
        private const string ClientId = "revit";
        private const string ClientSecret = "revit_secret";
        private const string Scope = "api openid offline_access";
        private const string AccessTokenEndpoint = "/v1/connect/token";
        private bool _isRunning;
        public MainForm()
        {
            InitializeComponent();
            InitWebBrowser();
            //webBrowser.DocumentCompleted += WebBrowser_DocumentCompleted;
        }
        private void InitWebBrowser()
        {
           
            webBrowser = new ChromiumWebBrowser();
            webBrowser.Dock = DockStyle.Fill;
            this.Controls.Add(webBrowser);
            
        }

       

        private async Task StartAuthorization()
        {
            string authorizationUri = $"{BaseUrl}{AuthorizationEndpoint}?client_id={ClientId}&redirect_uri={RedirectUri}&scope={Scope}&response_type=code";
            Process.Start(authorizationUri);

            // Start local HTTP server to capture redirect_uri
            await StartLocalHttpServer();
        }

        private async Task StartLocalHttpServer()
        {
            if(_isRunning) return;
            _isRunning=true;
            var listener = new HttpListener();
            listener.Prefixes.Add(RedirectUri);
            listener.Start();
            //to do port is used 

            // Handle incoming requests
            //while (true)
            //{
                var context = await listener.GetContextAsync();
                await HandleRedirect(context);
            //}
        }

        private async Task HandleRedirect(HttpListenerContext context)
        {
            // Extract authorization code from the query string
            string authorizationCode = HttpUtility.ParseQueryString(context.Request.Url.Query).Get("code");
            if (string.IsNullOrEmpty(authorizationCode))
            {
                return;
            }
            // Perform token exchange here using the authorization code
            var accessTokenUrl = $"{BaseUrl}{AccessTokenEndpoint}";
            await GetAccessToken(accessTokenUrl,authorizationCode);
            // Optionally, close the form or navigate to a different page
            //this.Close();
            HandleRedirectResponse(context);
        }
        private void HandleRedirectResponse(HttpListenerContext context)
        {
            HttpListenerResponse response = context.Response;

            // 设置响应头
            response.ContentType = "text/plain;charset=utf-8";
            response.StatusCode = (int)HttpStatusCode.OK;

            // 构建响应内容
            string responseString = "登录成功！请返回Revit";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

            // 设置响应内容长度
            response.ContentLength64 = buffer.Length;

            // 获取输出流并写入响应内容
            using (Stream output = response.OutputStream)
            {
                output.Write(buffer, 0, buffer.Length);
            }

            // 关闭输出流，完成响应
            response.Close();
        }
        public  async Task<string> GetAccessToken(string url, string code)
        {
            // 替换为你要访问的目标 URL
            string apiUrl = url;

            // 构造要发送的数据，可以是 JSON 字符串、表单数据等
            var postData = new Dictionary<string, string>
            {
                { "client_id", ClientId },
                { "client_secret", ClientSecret },
                { "code", code },
                { "grant_type", "authorization_code" },
                {"redirect_uri",RedirectUri }
            };

            // 创建 HttpClientHandler 实例
            var handler = new HttpClientHandler
            {
                // 设置 handler 信任服务器证书
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            using (var httpClient = new HttpClient(handler))
            {
                httpClient.Timeout = TimeSpan.FromMinutes(3);
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                var content= new FormUrlEncodedContent(postData);
                request.Content = new FormUrlEncodedContent(postData);

                // 发送 POST 请求
                var response = await httpClient.SendAsync(request);

                // 检查是否成功
                if (response.IsSuccessStatusCode)
                {
                    // 读取响应内容
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var data=JsonConvert.DeserializeObject<Dictionary<string,object>>(responseBody);
                    MessageBox.Show(responseBody);
                }
                else
                {
                    // 处理错误
                    Console.WriteLine("Error: " + response.StatusCode);
                }
            }
            return string.Empty;
        }


        private async void button1_Click(object sender, EventArgs e)
        {
            await StartAuthorization();
           
        }
    }

}


