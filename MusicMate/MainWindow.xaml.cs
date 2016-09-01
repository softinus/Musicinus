using HtmlAgilityPack;
using Microsoft.Win32;
using mshtml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MusicMate
{

    public class SongListItem
    {
        public string Name { get; set; }
        public string Artist { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string strMemberkey = string.Empty;
        public MainWindow()
        {
            SetBrowserFeatureControl();
            InitializeComponent();
            LoginStatus = ELoginStatus.NotReady;
        }


        public enum ELoginStatus
        {
            NotReady= 0,
            Ready,
            BeingProcessed,
            Logined
        }

        private ELoginStatus m_eLoginStatus = ELoginStatus.NotReady;  // login status;
        public ELoginStatus LoginStatus
        {
            get { return m_eLoginStatus; }
            set
            {
                m_eLoginStatus = value;
                lblStatus.Content = value.ToString();
            }
        }

        private void btnSignin_Click(object sender, RoutedEventArgs e)
        {
            txtID.IsEnabled = txtPW.IsEnabled = false;
            
            LoginStatus = ELoginStatus.BeingProcessed;
            HtmlElement btnElement1 = webTool.Document.GetElementById("id");
            HtmlElement btnElement2 = webTool.Document.GetElementById("pwd");

            btnElement1.Focus();
            SendKeys.SendWait(txtID.Text);
            btnElement2.Focus();
            SendKeys.SendWait(txtPW.Password);
            HtmlElement btnElement3 = webTool.Document.GetElementById("btnLogin");
            btnElement3.InvokeMember("click");

            //lstFavorites.Items.Clear();
            //GetFavoriteSongList();
        }

        private void btnGetList_Click(object sender, RoutedEventArgs e)
        {
            GetFavoriteSongList();
        }

        private void GetFavoriteSongList()
        {
            // Add columns
            var gridView = new GridView();
            this.lstFavorites.View = gridView;
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Name",
                DisplayMemberBinding = new System.Windows.Data.Binding("Name")
            });
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Artist",
                DisplayMemberBinding = new System.Windows.Data.Binding("Artist")
            });

            // Populate list
            HtmlWeb web = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument document = web.Load("http://www.melon.com/mymusic/like/mymusiclikesong_list.htm?memberKey=" + strMemberkey);
            //http://www.melon.com/mymusic/like/mymusiclikesong_list.htm?memberKey=7605160&orderBy=SUMM_CNT
            HtmlNode[] nodes = document.DocumentNode.SelectNodes("//tbody//a").ToArray();

            string strName = string.Empty;
            string strArtist = string.Empty;
            foreach (HtmlNode item in nodes)
            {
                if (item.InnerText.Contains("상세"))
                    continue;

                bool isAlreadyIncluded = false;
                foreach (SongListItem LSI in lstFavorites.Items)
                {
                    if (string.Compare(LSI.Name, item.InnerText) == 0)
                        isAlreadyIncluded = true;

                    if (string.Compare(LSI.Artist, item.InnerText) == 0)
                        isAlreadyIncluded = true;
                }

                if (string.Compare(strName, item.InnerText) == 0)
                    isAlreadyIncluded = true;

                if (isAlreadyIncluded)
                    continue;


                if (string.IsNullOrEmpty(strName) && string.IsNullOrEmpty(strArtist))
                {
                    strName = item.InnerText;
                }
                else if (string.IsNullOrEmpty(strName) == false && string.IsNullOrEmpty(strArtist))
                {
                    strArtist = item.InnerText;
                }

                // if both are not empty add one songlist in listview
                if (string.IsNullOrEmpty(strName) == false && string.IsNullOrEmpty(strArtist) == false)
                {
                    this.lstFavorites.Items.Add(new SongListItem { Name = strName, Artist = strArtist });
                    strName = string.Empty;
                    strArtist = string.Empty;
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.webTool.ScriptErrorsSuppressed = true;
            webTool.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(webBrowser_DocumentCompleted);
            webTool.Navigate("https://member.melon.com/muid/web/login/login_inform.htm");   // login page
        }

        private object InvokeScript()
        {
            HtmlElement head = webTool.Document.GetElementsByTagName("head")[0];
            HtmlElement scriptEl = webTool.Document.CreateElement("script");
            IHTMLScriptElement element = (IHTMLScriptElement)scriptEl.DomElement;
            element.text = "function invoke(method, args) { var context = window;var namespace = method.split('.');var func = namespace.pop();    for (var i = 0; i < namespace.length; i++) { context = context[namespace[i]];} result = context[func].apply(context, args);}";
            head.AppendChild(scriptEl);
            var parameters = new object[] { "pageObj.sendPage", "21" };
            var resultJson = webTool.Document.InvokeScript("invoke", parameters);
            return resultJson;
        }

        private void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            LoginStatus = ELoginStatus.Ready;
            btnSignin.IsEnabled = true;
            string strURL = webTool.Url.ToString();

            if (strURL == "https://member.melon.com/muid/web/login/login_inform.htm")
            {

            }
            if (strURL == "http://www.melon.com/")
            {
                webTool.Navigate("http://www.melon.com/mymusic/like/mymusiclikesong_list.htm?memberKey=7605160");
                //object result= webTool.Document.InvokeScript("MELON.WEBSVC.POC.menu.goMyMusicMain()"
                //    , new String[] { "LOG_PRT_CODE=1&MENU_PRT_CODE=0&MENU_ID_LV1=&CLICK_AREA_PRT_CODE=S01&ACTION_AF_CLICK=V1" });

                //if(result == null)
                //{
                //    System.Windows.MessageBox.Show(result.ToString());
                //}
            }
            else if(strURL.Contains("http://www.melon.com/mymusic/like/mymusiclikesong_list.htm?memberKey="))
            {
                LoginStatus = ELoginStatus.Logined;
                int nIndexOfDelm = strURL.IndexOf('=') + 1;
                strMemberkey = strURL.Substring(nIndexOfDelm, strURL.Length - nIndexOfDelm);

                btnGetList.IsEnabled = !string.IsNullOrEmpty(strMemberkey);
                //System.Windows.MessageBox.Show(strMemberkey);

                //var result= InvokeScript();
                //if (result == null)
                //{
                //    System.Windows.MessageBox.Show(result.ToString());
                //}
            }
            //HtmlElement btnElement2 = webTool.Document.All.GetElementsByName("memberPwd")[0];
            //var document = webTool.Document;
        }

        private void SetBrowserFeatureControl()
        {
            // http://msdn.microsoft.com/en-us/library/ee330720(v=vs.85).aspx

            // FeatureControl settings are per-process
            var fileName = System.IO.Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);

            // make the control is not running inside Visual Studio Designer
            if (String.Compare(fileName, "devenv.exe", true) == 0 || String.Compare(fileName, "XDesProc.exe", true) == 0)
                return;

            // TODO: FEATURE_BROWSER_MODE - what is it?
            SetBrowserFeatureControlKey("FEATURE_BROWSER_EMULATION", fileName, 9000); // Webpages containing standards-based !DOCTYPE directives are displayed in IE10 Standards mode.
            SetBrowserFeatureControlKey("FEATURE_DISABLE_NAVIGATION_SOUNDS", fileName, 1);
            SetBrowserFeatureControlKey("FEATURE_WEBOC_POPUPMANAGEMENT", fileName, 1);
            SetBrowserFeatureControlKey("FEATURE_BLOCK_INPUT_PROMPTS", fileName, 1);
        }
        private void SetBrowserFeatureControlKey(string feature, string appName, uint value)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(
                String.Concat(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\", feature),
                RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                key.SetValue(appName, (UInt32)value, RegistryValueKind.DWord);
            }
        }

        private void webTool_NewWindow(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }
    }
}
