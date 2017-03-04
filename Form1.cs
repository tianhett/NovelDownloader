using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using HtmlAgilityPack;

namespace NovelDownloader
{
    public partial class b : Form
    {
        public b()
        {
            InitializeComponent();
            m_context = SynchronizationContext.Current;
        }

        SynchronizationContext m_context = null;
        List<Thread> threadList = new List<Thread>();
        List<string> filePathList = new List<string>();
        public struct NovelDownloadStruct
        {
            public List<string> links;
            public string targetFile;
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            progressBar1.Value = 0;
            progressText.Text = "0%";
            if (!string.IsNullOrEmpty(txtSource.Text))
            {
                try
                {
                    string title = string.Empty;
                    if (txtSource.Text.LastIndexOf("/") <= -1)
                    {
                        txtSource.Text += "/";
                    }
                    string htmlText = GetHtmlString(txtSource.Text);
                    List<string> linkList = GetAllLinks(htmlText, ref title);
                    txtTarget.Text = Directory.GetCurrentDirectory() + "\\" + title + ".txt";
                    string targetFile = title + ".txt";
                    //DownloadAndMerge(linkList, targetFile);
                    NovelDownloadStruct novelStruct = new NovelDownloadStruct();
                    novelStruct.links = linkList;
                    novelStruct.targetFile = targetFile;
                    Thread thread = new Thread(DownloadAndMerge);
                    thread.IsBackground = true;
                    thread.Start(novelStruct);
                    threadList.Add(thread);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("检查地址是否正确");
                }
            }
            else
            {
                MessageBox.Show("小说目录地址不能为空");
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            //Application.Exit();
            if (threadList.Count > 0)
            {
                threadList.First().Abort();
            }
            DeleteAllFile();
            Environment.Exit(0);
        }

        private List<string> GetAllLinks(string htmlText, ref string title)
        {
            List<string> aLinks = new List<string>();
            Regex regTitle = new Regex(@"<title>(.*)</title>");
            var resTitle = regTitle.Match(htmlText).Groups;
            title = resTitle[1].ToString();
            Regex reg = new Regex(@"(?is)<a[^>]*?href=(['""]?)(?<url>[^'""\s>]+)\1[^>]*>(?<text>(?:(?!</?a\b).)*)</a>");
            MatchCollection mc = reg.Matches(htmlText);
            foreach (Match m in mc)
            {
                if (Path.GetExtension(m.Groups["url"].Value).IndexOf("html") > -1)
                {
                    Regex regPage = new Regex(@"第(\d+)章");
                    var result = regPage.Match(m.Groups["text"].Value).Groups;
                    if (result.Count > 1)
                    {
                        aLinks.Add(m.Groups["url"].Value);
                    }
                }
            }
            return aLinks;
        }

        private void DownloadAndMerge(object param)
        {
            NovelDownloadStruct novelStruct = (NovelDownloadStruct)param;
            List<string> links = novelStruct.links;
            string targetFile = novelStruct.targetFile;
            string dir = Directory.GetCurrentDirectory() + "\\" + DateTime.Now.ToString("yyyyMMddhhmmssffff");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            else
            {
                string cmd = " del /Q \"" + dir + "\\*.*\"";
                string output = "";
                CmdHelper.RunCmd(cmd, out output);
            }
            if (links.Count > 0)
            {
                double per = 0.95 / links.Count;
                for (int i = 0; i < links.Count; i++)
                {
                    string eachPath = string.Empty;
                    if (links[i].IndexOf("/") > -1)
                    {
                        Uri uri = new Uri(txtSource.Text);
                        eachPath = uri.Scheme + "://" + uri.Host + links[i].Replace(txtSource.Text,"/");
                    }
                    else
                    {
                        eachPath = txtSource.Text + Path.GetFileName(links[i]);
                    }
                    string htmlText = GetHtmlString(eachPath);
                    //htmlText = DealWithHtml(htmlText);
                    htmlText = NoHTML(htmlText);
                    string fileName = Path.GetFileNameWithoutExtension(eachPath);
                    string path = dir + "\\" + fileName + ".txt";
                    if (!filePathList.Contains(path))
                    {
                        filePathList.Add(path);
                        MergeText(htmlText, path);
                        int progress = (int)((i + 1) * per * 100);
                        m_context.Post(UpdateUI, progress);
                    }
                }
                //string cmd = @"copy *.txt temp.txt";
                string cmd = "copy \"" + dir + "\\*.txt\" \"" + dir + "\\temp.txt\"";
                string output = "";
                CmdHelper.RunCmd(cmd, out output);
                //File.Move("temp.txt", targetFile);
                File.Move(dir + "\\temp.txt", targetFile);
                DeleteAllFile();
                m_context.Post(UpdateUI, 100);
                MessageBox.Show("下载完成");
            }
        }

        private string NoHTML(string Htmlstring)
        {
            //删除脚本  
            Htmlstring = Regex.Replace(Htmlstring, @"<script[^>]*?>.*?</script>", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"<script[^>]*?>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
            //删除HTML的head
            Htmlstring = Regex.Replace(Htmlstring, @"<head>[\s\S]*</head>", "", RegexOptions.IgnoreCase);
            //删除HTML 
            Htmlstring = Regex.Replace(Htmlstring, @"<(.[^>]*)>", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"([\r\n])[\s]+", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"-->", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"<!--.*", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(quot|#34);", "\"", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(amp|#38);", "&", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(lt|#60);", "<", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(gt|#62);", ">", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(nbsp|#160);", "   ", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(iexcl|#161);", "\xa1", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(cent|#162);", "\xa2", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(pound|#163);", "\xa3", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(copy|#169);", "\xa9", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&#(\d+);", "", RegexOptions.IgnoreCase);
            Htmlstring.Replace("<", "");
            Htmlstring.Replace(">", "");
            Htmlstring.Replace("\r\n", "");
            Htmlstring = Htmlstring.Replace("<br/>", "").Replace("<br>", "").Trim();
            return Htmlstring;
        }

        //private string DealWithHtml(string Htmlstring)
        //{
        //    HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
        //    doc.LoadHtml(Htmlstring);

        //    return "";
        //}

        private void MergeText(string htmlText, string path)
        {
            try
            {
                FileStream fs = fs = new FileStream(path, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(htmlText);
                sw.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void DeleteAllFile()
        {
            if (filePathList.Count > 0)
            {
                foreach (var each in filePathList)
                {
                    File.Delete(each);
                }
            }
        }

        private void UpdateUI(object value)
        {
            progressBar1.Value = Convert.ToInt32(value);
            progressText.Text = string.Format("{0}%", value);
        }

        private string GetHtmlString(string url)
        {
            WebClient myWebClient = new WebClient();
            byte[] myDataBuffer = myWebClient.DownloadData(url);
            string strWebData = Encoding.Default.GetString(myDataBuffer);
            string pattern = @"(?i)\bcharset=(?<charset>[-a-zA-Z_0-9]+)";
            string charset = Regex.Match(strWebData, pattern).Groups["charset"].Value;
            if (!string.IsNullOrEmpty(charset) && Encoding.GetEncoding(charset) != Encoding.Default)
            {
                strWebData = Encoding.GetEncoding(charset).GetString(myDataBuffer);
            }
            else
            {
                strWebData = Encoding.Default.GetString(myDataBuffer);
            }
            return strWebData; 
        }

    }
}
