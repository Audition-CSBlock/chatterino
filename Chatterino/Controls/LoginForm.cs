﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.IO;
using Chatterino.Common;
using System.Text.Json;
using System.Net.NetworkInformation;

namespace Chatterino.Controls
{
    public partial class LoginForm : Form
    {
        HttpListener listener = new HttpListener();

        public LoginForm()
        {
            InitializeComponent();

            //Height = 225;

            Icon = App.Icon;


            Task.Run(() =>
            {
                try
                {
                    listener.Prefixes.Add("http://127.0.0.1:5215/");
                    listener.Start();

                    while (listener.IsListening)
                    {
                        var context = listener.GetContext();

                        if (context.Request.Url.AbsolutePath == "/code")
                        {
                            string answer = $@"<html>
<head>
    <title>chatterino login</title>
</head>
<body>
    <h1>Redirecting</h1>
    <p>If your webbrowser does not redirect you automatically, click <a id='link'>here</a>.</p>
    <script type='text/javascript'>
        var link = 'http://127.0.0.1:5215/token?' + location.hash.substring(1);
        window.location = link;
        document.getElementById('link').href = link; 
    </script>
</body>
</html>";

                            byte[] bytes = Encoding.UTF8.GetBytes(answer);

                            context.Response.ContentLength64 = bytes.Length;
                            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            context.Response.OutputStream.Flush();
                            context.Response.Close();
                        }
                        else if (context.Request.Url.AbsolutePath == "/token")
                        {
                            var access_token = context.Request.QueryString["access_token"];
                            var scope = context.Request.QueryString["scope"];

                            WebRequest request = WebRequest.Create("https://api.twitch.tv/kraken?oauth_token=" + access_token + "&client_id=7ue61iz46fz11y3cugd0l3tawb4taal");
                            using (var response = request.GetResponse())
                            using (var stream = response.GetResponseStream())
                            {
                                JsonParser parser = new JsonParser();
                                dynamic json = parser.Parse(stream);
                                dynamic token = json["token"];
                                string username = token["user_name"];

                                using (var writer = File.CreateText("./login.ini"))
                                {
                                    writer.Write("username=");
                                    writer.WriteLine(username);

                                    writer.Write("oauth=");
                                    writer.WriteLine(access_token);
                                }
                            }

                            string answer = $@"<html>
<head>
    <title>chatterino login</title>
</head>
<body>
    <h1>Login Successful</h1>
    <p>You can now close this page and continue using chatterino.</p>
</body>
</html>";

                            byte[] bytes = Encoding.UTF8.GetBytes(answer);

                            context.Response.ContentLength64 = bytes.Length;
                            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            context.Response.OutputStream.Flush();
                            context.Response.Close();

                            IrcManager.Connect();

                            this.Invoke(() => Close());
                        }
                    }
                }
                catch
                {
                    buttonLogin.Invoke(() =>
                    {
                        buttonLogin.Enabled = false;
                        lblError.Visible = true;
                    });
                }
            });
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            listener.Close();

            base.OnClosing(e);
        }

        private void buttonLogin_Click(object sender, EventArgs e)
        {
            IrcManager.Disconnect();

            Process.Start("https://api.twitch.tv/kraken/oauth2/authorize?response_type=token&client_id=7ue61iz46fz11y3cugd0l3tawb4taal&redirect_uri=http://127.0.0.1:5215/code&force_verify=true&scope=chat_login+user_subscriptions+user_blocks_edit+user_blocks_read+user_follows_edit");
        }

        private void btnManualLogin_Click(object sender, EventArgs e)
        {
            Process.Start("https://api.twitch.tv/kraken/oauth2/authorize?response_type=code&client_id=gkp8i0oxk7xua6pcxmg4w6u8vt8n4qw&redirect_uri=https%3A%2F%2Ffourtf.com%2Fchatterino%2Fauth&force_verify=true&scope=chat_login+user_subscriptions+user_blocks_edit+user_blocks_read+user_follows_edit");
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            //if (checkBox1.Checked)
            //{
            //    Height = 374;
            //}
            //else
            //{
            //    Height = 225;
            //}
        }
    }
}
