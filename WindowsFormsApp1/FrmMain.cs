﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using NewLife.Collections;
using NewLife.IO;
using NewLife.Log;
using NewLife.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WindowsFormsApp1
{
    [ComVisible(true)]
    public partial class FrmMain : Form
    {
        private ChromiumWebBrowser Browser { get; set; }

        public FrmMain()
        {
            InitializeComponent();

            InitBrowser();
        }

        public void InitBrowser()
        {
            var settines = new CefSettings()
            {
                Locale = "zh-CN",
                AcceptLanguageList = "zh-CN",
                MultiThreadedMessageLoop = true
            };

            Cef.Initialize(settines);

            var bw = new ChromiumWebBrowser("");
            panel1.Controls.Add(bw);
            bw.Dock = DockStyle.Fill;

            var req = new MyRequestHandler
            {
                OnComplete = OnComplete
            };

            bw.RequestHandler = req;

            bw.FrameLoadStart += Browser_FrameLoadStart;
            bw.FrameLoadEnd += Web_FrameLoadEnd;
            bw.StatusMessage += Bw_StatusMessage;
            bw.TitleChanged += Bw_TitleChanged;

            Application.ApplicationExit += (s, e) => Cef.Shutdown();

            Browser = bw;
        }

        private void Bw_TitleChanged(Object sender, TitleChangedEventArgs e)
        {
            this.Invoke(() => Text = e.Title);
        }

        private void Bw_StatusMessage(Object sender, StatusMessageEventArgs e)
        {
            this.Invoke(() => lbStatus.Text = e.Value);
        }

        private void Form1_Load(Object sender, EventArgs e)
        {
#if DEBUG
            var result = File.ReadAllText("result.json");
            DecodeResult(result);
#endif
        }

        private void Browser_FrameLoadStart(Object sender, FrameLoadStartEventArgs e)
        {
            XTrace.WriteLine("FrameLoadStart {0}", e.Url);
        }

        private void BtnGo_Click(Object sender, EventArgs e)
        {
            Browser.Load(txtUrl.Text);
        }

        private void Web_FrameLoadEnd(Object sender, FrameLoadEndEventArgs e)
        {
            XTrace.WriteLine("FrameLoadEnd {0}", e.Url);

            //var url = e.Url;
            //var result = await Browser.GetSourceAsync();
            //var html = result;
        }

        private void OnComplete(IRequest request, IResponse response, String result)
        {
            // 解码Json
            ThreadPoolX.QueueUserWorkItem(DecodeResult, result);
        }

        private void DecodeResult(String result)
        {
            var js = JsonConvert.DeserializeObject(result);

            //var js = new JsonParser(result).Decode();
            Decode(js);
        }

        private void Decode(Object js)
        {
            //if (js is IList<Object> list)
            //{
            //    if (list.Count > 0) WriteData(list);

            //    return;
            //}

            //if (js is IDictionary<String, Object> dic)
            //{
            //    foreach (var item in dic)
            //    {
            //        Decode(item.Value);
            //    }
            //}

            if (js is IDictionary<String, JToken> jts && jts.Count > 0)
            {
                foreach (var item in jts)
                {
                    Decode(item.Value);
                }

                return;
            }

            if (js is IList<JToken> tokens)
            {
                if (tokens.Count > 0) WriteData(tokens);

                return;
            }
        }

        private static Int32 _gid;
        private void WriteData(IList<JToken> list)
        {
            //var sb = Pool.StringBuilder.Get();
            //var lines = new List<String>();

            // 头部
            var headers = new List<String>();
            foreach (var item in list)
            {
                if (item is IDictionary<String, JToken> dic)
                {
                    foreach (var elm in dic)
                    {
                        if (!headers.Contains(elm.Key)) headers.Add(elm.Key);
                    }
                }
            }

            var fname = $"{DateTime.Now:yyyyMMddHHmmss}_{++_gid}.csv";
            using (var csv = new CsvFile(fname, true))
            {
                //csv.Encoding = new UTF8Encoding(true);

                // 第一行写头部
                //if (lines.Count == 0) lines.Add(headers.Join(","));
                csv.WriteLine(headers);

                // 单行和多行
                foreach (var item in list)
                {
                    if (item is IDictionary<String, JToken> dic)
                    {
                        csv.WriteLine(headers.Select(e => dic[e]));
                    }
                    else
                    {
                        //lines.Add(item + "");
                        csv.WriteLine(new[] { item });
                    }
                }
            }

            //var fname = $"{DateTime.Now:yyyyMMddHHmmss}_{++_gid}.csv";
            //File.WriteAllLines(fname, lines, new UTF8Encoding(true));
        }
    }
}