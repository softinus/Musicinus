using HtmlAgilityPack;
using Microsoft.Win32;
using mshtml;
using MusicMate.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        public string Album { get; set; }
    }

    public static class UserInfo
    {
        public static string strMemberkey { get; set; }
        public static string strNickName { get; set; }
        public static string strDescription { get; set; }
        public static int nTotalLikeSongs { get; set; }
        public static int nTotalPlaylists { get; set; }
        public static int nTotalFriendLists { get; set; }

        public static string BuildTheString(string _strDelimeter)
        {
            string strRes = string.Empty;
            strRes += strMemberkey + _strDelimeter;
            strRes += strNickName + _strDelimeter;
            strRes += strDescription + _strDelimeter;
            strRes += nTotalLikeSongs + _strDelimeter;
            strRes += nTotalPlaylists + _strDelimeter;
            strRes += nTotalFriendLists;

            return strRes;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        StringBuilder sb = new StringBuilder();

        bool bMultipleScrapMode = false;
        bool bDiscoverNonExistMember = false;
        bool bTryToStop = true;
        const string strInitURL = "https://member.melon.com/muid/web/login/login_inform.htm";



        
        public MainWindow()
        {
            SetBrowserFeatureControl();
            InitializeComponent();
            LoginStatus = ELoginStatus.NotReady;

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
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Album",
                DisplayMemberBinding = new System.Windows.Data.Binding("Album")
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = new VM_Chart();

            this.webTool.ScriptErrorsSuppressed = true;
            webTool.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(webBrowser_DocumentCompleted);
            webTool.Navigate(strInitURL);   // login page

            //webTool.ProgressChanged += new WebBrowserProgressChangedEventHandler(_ie);

    }


        #region propperty for login status
        public enum ELoginStatus
        {
            NotReady= 0,
            ReadyToFind,
            BeingProcessed,
            SeekingYourMusicRoom,
            SeekingYourFavoriteMusics,
            MakingListOfYourFavoriteSongs,
            EverythingFound,
            FailedWrongIDorPassword,
            FailedExpireID
        }

        private ELoginStatus m_eLoginStatus = ELoginStatus.NotReady;  // login status;
        public ELoginStatus LoginStatus
        {
            get { return m_eLoginStatus; }
            set
            {
                m_eLoginStatus = value;
                btnStatus.Content = value.ToString();

                
                if (value != ELoginStatus.NotReady) // scrap mode activate only when is ready
                {
                    btnScrapMode.IsEnabled = true;
                }
                else
                    btnScrapMode.IsEnabled = false;


                if (value == ELoginStatus.NotReady ||
                   value == ELoginStatus.ReadyToFind ||
                   value == ELoginStatus.EverythingFound)   // stop button de-activate when the app found sth or not ready
                {
                    btnScrapModeStop.IsEnabled = false;
                    
                }
                else
                {
                    btnScrapModeStop.IsEnabled = true;
                    
                }
            }
        }
        #endregion

        private IEnumerable<HtmlElement> ElementsByClass(System.Windows.Forms.HtmlDocument doc, string className)
        {
            foreach (HtmlElement e in doc.All)
                if (e.GetAttribute("className") == className)
                    yield return e;
        }



        private void Login()
        {
            txtID.IsEnabled = txtPW.IsEnabled = false;
            btnSignin.IsEnabled = false;

            LoginStatus = ELoginStatus.BeingProcessed;
            HtmlElement btnElement1 = webTool.Document.GetElementById("id");
            HtmlElement btnElement2 = webTool.Document.GetElementById("pwd");

            btnElement1.Focus();
            SendKeys.SendWait(txtID.Text);
            btnElement2.Focus();
            SendKeys.SendWait(txtPW.Password);
            HtmlElement btnElement3 = webTool.Document.GetElementById("btnLogin");
            btnElement3.InvokeMember("click");
        }

        private void btnSignin_Click(object sender, RoutedEventArgs e)
        {
            this.Login();
        }

        private void btnGetList_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show(GetTheFirstNumberOfSongOfCurrentList().ToString());
        }


        private void btnScrapMode_Click(object sender, RoutedEventArgs e)
        {
            if (bMultipleScrapMode == false)
            {
                if(string.IsNullOrEmpty( txtStartRange.Text.Trim()) ||
                    string.IsNullOrEmpty(txtEndRange.Text.Trim()))
                {
                    System.Windows.Forms.MessageBox.Show("Fill in the numbers.");
                    return;
                }
                txtStartRange.IsEnabled = false;
                txtEndRange.IsEnabled = false;

                int nStartRange = Convert.ToInt32(txtStartRange.Text);
                int nEndRange = Convert.ToInt32(txtEndRange.Text);
                MutipleScrapByRange(nStartRange, nEndRange);
            }
        }


        private void btnScrapModeStop_Click(object sender, RoutedEventArgs e)
        {   // request to stop
            bTryToStop = true;
        }


        private void MutipleScrapByRange(int _nMin, int _nMax)
        {
            if(_nMax <= _nMin)
            {
                System.Windows.Forms.MessageBox.Show("Mininum number should be lower than maxinum number.");
                return;
            }

            sb.AppendLine("key|nickname|description|total_songs|total_playlists|total_friends|song_name|artist|album");
            bMultipleScrapMode = true;
            for (int i=_nMin; i<=_nMax; ++i)
            {
                ClearTheData();
                LoginStatus = ELoginStatus.ReadyToFind;
                
                webTool.Navigate("http://www.melon.com/mymusic/main/mymusicmainother_list.htm?memberKey=" + i);

                while (LoginStatus != ELoginStatus.EverythingFound) // wait until everything is done
                {
                    System.Windows.Forms.Application.DoEvents();
                    if (bDiscoverNonExistMember)
                    {
                        bDiscoverNonExistMember = false;
                        break;
                    }
                }
            }
            bMultipleScrapMode = false;
            File.WriteAllText("scrapped_"+txtStartRange.Text + "_to_"+txtEndRange.Text+".csv", sb.ToString(), Encoding.UTF8);
            sb.Clear();
        }

        private void ClearTheData()
        {
            VM_Chart.AnalData1.Clear();
            lstFavorites.Items.Clear();
        }

        private int GetTheFirstNumberOfSongOfCurrentList()
        {
            if(webTool.Url.ToString().Contains(strInitURL))
            {
                LoginStatus = ELoginStatus.ReadyToFind;
            }
            else if (webTool.Url.ToString().Contains("http://www.melon.com/mymusic/like/mymusiclikesong_list.htm?memberKey="))
            {
                string strNum = "";
                // Populate list
                List<HtmlElement> arrElements = new List<HtmlElement>(webTool.Document.GetElementsByTagName("tr").Cast<HtmlElement>());
                foreach (HtmlElement EI in arrElements)
                {
                    List<HtmlElement> arrElements2 = new List<HtmlElement>(EI.GetElementsByTagName("div").Cast<HtmlElement>());
                    if (arrElements2.Count == 16 || arrElements2.Count == 19)
                    {
                        strNum = arrElements2[1].InnerText;
                        break;
                    }
                }
                return Convert.ToInt32(strNum);
            }
            return -1;
        }
        

        private void GetFavoriteSongList()
        {
            const int nSongCountOfEachPage = 20;
            int nTotalCount = UserInfo.nTotalLikeSongs;//Convert.ToInt32(webTool.Document.GetElementById("totCnt").InnerText);
            int nPageCount = (int)Math.Ceiling((double)nTotalCount / nSongCountOfEachPage);

            PGB_browser.Maximum = nPageCount; 
            for(int i=0; i<nPageCount; ++i)
            {
                if (bTryToStop)
                {
                    return;
                }

                int TargetPage = (i * nSongCountOfEachPage + 1);
                string str = "javascript: pageObj.sendPage('" + TargetPage + "');";
                webTool.Navigate(str);

                while (GetTheFirstNumberOfSongOfCurrentList() != TargetPage)
                    System.Windows.Forms.Application.DoEvents();

                PGB_browser.Value = i;

                // Populate list
                List<HtmlElement> arrElements = new List<HtmlElement>(webTool.Document.GetElementsByTagName("tr").Cast<HtmlElement>());
                foreach (HtmlElement EI in arrElements)
                {
                    List<HtmlElement> arrElements2 = new List<HtmlElement>(EI.GetElementsByTagName("a").Cast<HtmlElement>());
                    //List<HtmlElement> arrElements2_ = new List<HtmlElement>(EI.GetElementsByTagName("div").Cast<HtmlElement>());
                    if (arrElements2.Count == 5)
                    {
                        string strName = arrElements2[1].InnerText;
                        string strArtist = arrElements2[2].InnerText;
                        string strAlbum = arrElements2[4].InnerText;
                        this.lstFavorites.Items.Add(new SongListItem { Name = strName, Artist = strArtist, Album = strAlbum });

                        this.RelectingToTheDictionary(strName, strArtist);


                    }
                }
            }
            LoginStatus = ELoginStatus.EverythingFound;


            var myList = m_dArtistSeries.ToList();
            myList.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

            {
                foreach (SongListItem SLI in lstFavorites.Items)
                {
                    sb.AppendLine(UserInfo.BuildTheString("|")+
                        "|"+ SLI.Name +"|"+ SLI.Artist +"|"+ SLI.Album);
                }
            }

            // during the multiple scrapping mode, chart is disabled.
            if(bMultipleScrapMode == false)
            {
                int nCount = 0;
                const int nMAX = 5;
                foreach (KeyValuePair<string, int> entry in myList)
                {
                    if (nMAX == nCount++)
                        break;

                    VM_Chart.AnalData1.Add(new Artists() { Category = entry.Key, Number = entry.Value });
                }
                GridForPieChart.Visibility = Visibility.Visible;
            }

        }

        private Dictionary<string, int> m_dArtistSeries = new Dictionary<string, int>();
        private void RelectingToTheDictionary(String strName, String strArtist)
        {
            int nValue;
            if (m_dArtistSeries.TryGetValue(strArtist, out nValue))
            {
                Console.WriteLine("Fetched value: {0}", nValue);
                m_dArtistSeries[strArtist] = nValue + 1;
            }
            else
            {
                Console.WriteLine("No such key: {0}", strArtist);
                m_dArtistSeries.Add(strArtist, 1);
            }
        }
        
        private int ConvertOnlyNumber(string _strTarget)
        {
            string resultString = null;
            try
            {
                Regex regexObj = new Regex(@"[^\d]");
                resultString = regexObj.Replace(_strTarget, "");
            }
            catch (ArgumentException ex)
            {
                // Syntax error in the regular expression
                return -1;
            }
            return Convert.ToInt32(resultString);
        }
        

        private void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            //SendKeys.Send("{ENTER}");
            //LoginStatus = ELoginStatus.ReadyToFind;

            string strURL = e.Url.ToString();

            if (strURL == "https://member.melon.com/muid/web/login/login_inform.htm")
            {
                if(bTryToStop)
                {
                    bTryToStop = false;
                    LoginStatus = ELoginStatus.ReadyToFind;
                }
                btnSignin.IsEnabled = true;
                List<HtmlElement> arrElements = ElementsByClass(webTool.Document, "txt_error").ToList();
                if (arrElements.Count != 0)
                {   // if there's error, prints error context and enable id, pw input box again.
                    System.Windows.MessageBox.Show(arrElements[0].InnerText);
                    txtID.IsEnabled = txtPW.IsEnabled = true;
                    btnSignin.IsEnabled = true;
                    txtPW.Focus();
                    txtPW.SelectAll();
                    LoginStatus = ELoginStatus.FailedWrongIDorPassword;
                }
            }
            if (strURL == "http://www.melon.com/")
            {
                if(bTryToStop)
                {
                    webTool.Navigate("https://member.melon.com/muid/web/login/login_inform.htm");
                    return;
                }
                else if(bMultipleScrapMode) // when the scrapper discovers non-member's id
                {
                    bDiscoverNonExistMember = true;
                    return;
                }
                LoginStatus = ELoginStatus.SeekingYourMusicRoom;
                this.webTool.Navigate("javascript: MELON.WEBSVC.POC.menu.goMyMusicMain();");
            }
            else if (strURL.Contains("http://www.melon.com/mymusic/main/mymusicmain_list.htm?memberKey=") ||// when retreiving my music room
                strURL.Contains("http://www.melon.com/mymusic/main/mymusicmainother_list.htm?memberKey="))  // when retreiving another melon user's music room
            {
                LoginStatus = ELoginStatus.SeekingYourFavoriteMusics;
                int nIndexOfDelm = strURL.IndexOf('=') + 1;
                UserInfo.strMemberkey = strURL.Substring(nIndexOfDelm, strURL.Length - nIndexOfDelm);
                

                List<HtmlElement> arrUserInfo = ElementsByClass(webTool.Document, "nicnmname").ToList();
                if (arrUserInfo.Count != 0)
                {
                    List<HtmlElement> arrHE1 = new List<HtmlElement>(arrUserInfo[0].GetElementsByTagName("dt").Cast<HtmlElement>());
                    List<HtmlElement> arrHE2 = new List<HtmlElement>(arrUserInfo[0].GetElementsByTagName("dd").Cast<HtmlElement>());
                    List<HtmlElement> arrHE3 = new List<HtmlElement>(arrUserInfo[0].GetElementsByTagName("a").Cast<HtmlElement>());

                    if (arrHE1.Count != 0)
                    {
                        UserInfo.strNickName= arrHE1[0].InnerText;
                    }
                    if (arrHE2.Count != 0)
                    {
                        UserInfo.strDescription = arrHE2[0].InnerText;
                    }
                    if (arrHE3.Count >= 3)
                    {

                        UserInfo.nTotalLikeSongs = ConvertOnlyNumber(arrHE3[0].InnerText);
                        UserInfo.nTotalPlaylists = ConvertOnlyNumber(arrHE3[1].InnerText);
                        UserInfo.nTotalFriendLists = ConvertOnlyNumber(arrHE3[2].InnerText);
                    }
                    txtUserInfo.Text = UserInfo.BuildTheString("\r\n");
                }

                webTool.Navigate("javascript: mymusic.mymusicLink.goLikeSong('" + UserInfo.strMemberkey + "');");
            }
            else if (strURL.Contains("http://www.melon.com/mymusic/like/mymusiclikesong_list.htm?memberKey="))
            {
                LoginStatus = ELoginStatus.MakingListOfYourFavoriteSongs;

                GetFavoriteSongList();

                //btnGetList.IsEnabled = !string.IsNullOrEmpty(strMemberkey);
            }
            else if (strURL.Contains("https://member.melon.com/muid/web/login/login_informExpire.htm"))
            {
                LoginStatus = ELoginStatus.FailedExpireID;
                
                System.Windows.MessageBox.Show("비밀번호를 5회 이상 잘못 입력하셨습니다.\n홈페이지에서 비밀번호 찾기를 통해 본인확인 후 비밀번호를 재설정하여 이용하시기 바랍니다.\n변경 후, 프로그램은 재시작해주세요.");
            }
                
            else
            {
                Debug.Print(strURL);
            }
        }

        #region Browser feature controls
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
        #endregion

        private void webTool_NewWindow(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }

        // while enter the password, press enter.
        private void txtPW_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                this.Login();
                //Console.WriteLine("Entered the password");
            }
        }
        
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

}
}
