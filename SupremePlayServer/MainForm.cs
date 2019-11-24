using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace SupremePlayServer
{
    public partial class MainForm : Form
    {
        public List<UserThread> UserList;
        public Dictionary<String, List<string>> MapUser = new Dictionary<string, List<string>>();
        
       
        public MainForm()
        {
            InitializeComponent();
            
            radioButton1.Select(); // 경험치 이벤트 없음
            radioButton_1.Select(); // 처음에 공지로 미리 선택됨


            List<String> a = new List<string> {"0"};
            MapUser.Add("0", a);

            // 타이머 생성 및 시작
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000; // 몹 리젠 시간
            timer.Tick += new EventHandler(timer_tick);
            timer.Start();

            // 서버 시작할 때 몹 데이터 정리
            string t = DateTime.Now.ToString();
            System_DB system_db = new System_DB();
            system_db.DelAllMonster();
            listBox2.Items.Add("<" + t + " 서버 시작>");
            listBox2.Items.Add("[" + t + "] 몬스터 데이터 삭제");
        }

        void timer_tick(object sender, EventArgs e)
        {
            try
            {
                string t = DateTime.Now.ToString("HH:mm:ss");
                label3.Text = "현재 시간 : " + t;
                // 초당 몬스터 db에서 체력 0인 몹의 리젠 시간을 줄인다.
                System_DB system_db = new System_DB();
                system_db.respawnMonster();

                if(t.Contains(":00:00") || t.Contains(":00:01"))
                {
                    listBox2.Items.Add("[" + t + "] 맵의 모든 아이템 삭제");
                    Packet("<chat>맵의 모든 아이템들이 삭제 됩니다.</chat>");

                    system_db.DelAllItem();
                }
            }
            catch
            {
                MessageBox.Show(e.ToString());
            }
        }
        

        private void Form1_Load(object sender, EventArgs e)
        {
            // Initialize
            UserList = new List<UserThread>();

            // Listen New User Connection
            Thread echo_thread = new Thread(Thread_NetWorkListening);
            echo_thread.Start();
        }

        #region Mulit-Thread Tcp/Ip Network

        // Listen New User Connection
        public void Thread_NetWorkListening()
        {
            TcpListener Listener = null;
            TcpClient client = null;

            try
            {
                Listener = new TcpListener(IPAddress.Any, Int32.Parse(Properties.Resources.PORT));
                Listener.Start(); // Listener 동작 시작

                while (true)
                {
                    // Accept New Tcp Client
                    client = Listener.AcceptTcpClient();

                    // New Client User Thread
                    UserThread userthread = new UserThread();
                    userthread.mainform = this;
                    userthread.startClient(client);
                    UserList.Add(userthread);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            finally
            {

            }
        }

        #endregion
        // 모든 유저에게 전송하는 패킷
        public void Packet(String data, String userCode = "")
        {
            for (int i = 0; i < UserList.Count; i++)
            {
                if (!UserList[i].UserCode.Equals("*null*"))
                {
                    if (UserList[i].UserCode.Equals(userCode))
                        continue;
                    try
                    {
                        //MessageBox.Show(data);
                        UserList[i].SW.WriteLine(data); // 메시지 보내기
                        UserList[i].SW.Flush();
                    }
                    catch (Exception e) // 팅긴걸로 판단
                    {
                        removethread(UserList[i]);
                    }
                }
                // 유효하지 않은 유저는 삭제
                else
                {
                    removethread(UserList[i]);
                }
            }
            PlayerCount();

            
            if (data.Contains("<chat1>"))
            {
                string[] word = splitTag("chat1", data).Split(',');
                listBox2.Items.Add("[" + DateTime.Now.ToString() + "] " + word[0]);
                int visibleItems = listBox2.ClientSize.Height / listBox2.ItemHeight;
                listBox2.TopIndex = Math.Max(listBox2.Items.Count - visibleItems + 1, 0);   
            }
            if (data.Contains("<chat>"))
            {
                string[] word = splitTag("chat", data).Split(',');
                listBox2.Items.Add("[" + DateTime.Now.ToString() + "] " + word[0]);
                int visibleItems = listBox2.ClientSize.Height / listBox2.ItemHeight;
                listBox2.TopIndex = Math.Max(listBox2.Items.Count - visibleItems + 1, 0);
            }
        }

        // 유저 리스트에서 제거한다.
        public void removethread(UserThread userthread)
        {
            try
            {
                // 접속이 되지 않은 유저 삭제 : 중간에 팅긴 유저에 대한 처리
                if (!userthread.client.Connected) // 접속이 끊겼는데 접속 되어 있다고 처리되서 계속 오류나고 있음
                {
                    if (userthread.UserName != null)
                    {
                        UserList.Remove(userthread);
                        PlayerCount();
                        userthread.thread.Abort();
                        if (userthread.UserName != null)
                        {   
                            Packet("<9>" + userthread.UserCode + "</9>");
                        }
                    }

                    if (userthread.thread != null)
                    {
                        UserList.Remove(userthread); // 여기서 문제인건데...
                        PlayerCount();
                        userthread.thread.Abort();
                        if (userthread.UserName != null)
                        {
                            //MessageBox.Show(userthread.UserName + "'님께서 종료하셨습니다.");
                            listBox2.Items.Add("(" + DateTime.Now.ToString() + ") " + userthread.UserName + " 종료");
                            Packet("<chat1>(알림): '" + userthread.UserName + "'님께서 종료하셨습니다.</chat1>");
                            Packet("<9>" + userthread.UserCode + "</9>");
                        }
                    }

                    if (MapUser.ContainsKey(userthread.last_map_id))
                    {
                        string last = userthread.last_map_id;
                        if (MapUser[last].Contains(userthread.UserCode))
                        {
                            if (MapUser[last].Count >= 2 && MapUser[last].IndexOf(userthread.UserCode) == 0)
                            {
                                for (int i = 1; i < UserList.Count; i++)
                                {
                                    if (UserList[i].UserCode.Equals(MapUser[last][1]))
                                    {
                                        try
                                        {
                                            UserList[i].SW.WriteLine("<map_player>1</map_player>"); // 메시지 보내기
                                            UserList[i].SW.Flush();
                                            break;
                                        }
                                        catch (Exception e) // 팅긴걸로 판단
                                        {
                                            //MessageBox.Show(e.ToString());
                                        }
                                    }
                                    // 유효하지 않은 유저는 삭제
                                    else
                                    {
                                        
                                    }
                                }
                            }
                            MapUser[last].Remove(userthread.UserCode);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        // 접속중인 아이디를 체크한다.
        public bool Checkid(String id)
        {
            bool check = false;

            try
            {
                for (int i = 0; i < UserList.Count; i++)
                {
                    if (UserList[i].UserId != null)
                    {
                        if (UserList[i].UserId.Equals(id))
                            check = true;
                    }
                }
            }
            catch (Exception e)
            {
                //MessageBox.Show(e.ToString());
            }

            return check;
        }

        private void FormClose(object sender, FormClosedEventArgs e)
        {
            string dir = "./Log/";
            // 지금까지의 로그를 저장하기
            try
            {
                if (!Directory.Exists(@dir)) 
                    Directory.CreateDirectory(@dir);
            }
            catch
            {

            }
            listBox2.Items.Add("<" + DateTime.Now.ToString() + " 서버 종료>");
            listBox2.Items.Add("");
            string t = DateTime.Now.ToShortDateString();
            using (StreamWriter logfile = new StreamWriter(@dir + "(" + t + ")Log.txt", true))
            {
                foreach (string i in listBox2.Items)
                {
                    logfile.WriteLine(i.ToString());
                }
            }
            Application.ExitThread();
            Environment.Exit(0);
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        private void PlayerCount()
        {
            toolStripStatusLabel2.Text = "접속자 수 : " + UserList.Count;

            listBox1.Items.Clear();
            for (int i = 0; i < UserList.Count; i++)
            {
                listBox1.Items.Add(UserList[i].UserName + "(" + UserList[i].UserId + ")");
            }
        }

        // Split Tag
        public String splitTag(String tag, String data)
        {
            string[] co1 = { "<" + tag + ">" };
            String[] d1 = data.Split(co1, StringSplitOptions.RemoveEmptyEntries);

            string[] co2 = { "</" + tag + ">" };
            String[] d2 = d1[0].Split(co2, StringSplitOptions.RemoveEmptyEntries);

            return d2[0];
        }

        public int radioSelected()
        {
            if(radioButton1.Checked)
            {
                return 0; // 경험치 이벤 없음
            }
            else if (radioButton2.Checked)
            {
                return 2; // 경험치 2배
            }
            else if (radioButton3.Checked)
            {
                return 3; // 경험치 3배
            }
            else if (radioButton4.Checked)
            {
                return 5; // 경험치 5배
            }
            return 0;
        }

        // 공지 보내기
        private void button1_Click(object sender, EventArgs e)
        {
            if (radioButton_1.Checked) // 공지
            {
                if (!textBox1.Text.Equals(""))
                {
                    Packet("<chat>(공지) : " + textBox1.Text + "</chat>");
                    textBox1.Text = "";
                }
            }
            else if (radioButton_2.Checked) // 감옥
            {
                if (listBox1.SelectedIndex >= 0)
                {
                    string name = UserList[listBox1.SelectedIndex].UserName;
                    Packet("<prison>" + name + "</prison>");
                    listBox2.Items.Add("(" + DateTime.Now.ToString() + ") " + name + " 감옥");
                    textBox1.Text = "";
                }
            }
            else if (radioButton_3.Checked) // 석방
            {
                if (listBox1.SelectedIndex >= 0)
                {
                    string name = UserList[listBox1.SelectedIndex].UserName;
                    Packet("<emancipation>" + name + "</emancipation>");
                    listBox2.Items.Add("(" + DateTime.Now.ToString() + ") " + name + " 석방");
                    textBox1.Text = "";
                }
            }
            else if (radioButton_4.Checked) // 유저 강퇴
            {
                if (listBox1.SelectedIndex >= 0 && !textBox1.Text.Equals(""))
                {
                    string name = UserList[listBox1.SelectedIndex].UserName;
                    if (textBox1.Text == "")
                        Packet("<ki>" + name + ",강퇴 당하셨습니다.,</ki>");
                    else
                        Packet("<ki>" + name + "," + textBox1.Text + ",</ki>");
                    Packet("<chat>" + name + "님이 강퇴 당하셨습니다." + "</chat>");
                    listBox2.Items.Add("(" + DateTime.Now.ToString() + ") " + name + " 강퇴");
                    textBox1.Text = "";
                }
            }
            else if (radioButton_5.Checked) // 모두 강퇴
            {
                Packet("<ki>모두," + textBox1.Text + ",</ki>");
                listBox2.Items.Add("(" + DateTime.Now.ToString() + ") 모두 강퇴 : " + textBox1.Text);
                textBox1.Text = "";
            }
            // 자동 스크롤
            int visibleItems = listBox2.ClientSize.Height / listBox2.ItemHeight;
            listBox2.TopIndex = Math.Max(listBox2.Items.Count - visibleItems + 1, 0);
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            
        }
        
        private void message_keyDown(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                this.button1_Click(sender, e);
            }
        }

        private void RadioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked == true && UserList != null)
            {
                listBox2.Items.Add("[" + DateTime.Now.ToString() + "] 경험치 이벤트 종료");
                Packet("<exp_event> 0 </exp_event>");
            }
        }

        private void RadioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked == true && UserList != null)
            {
                listBox2.Items.Add("[" + DateTime.Now.ToString() + "] 경험치 2배 이벤트 시작");
                Packet("<exp_event> 2 </exp_event>");
            }
        }

        private void RadioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked == true && UserList != null)
            {
                listBox2.Items.Add("[" + DateTime.Now.ToString() + "] 경험치 3배 이벤트 시작");
                Packet("<exp_event> 3 </exp_event>");
            }
        }

        private void RadioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton4.Checked == true && UserList != null)
            {
                listBox2.Items.Add("[" + DateTime.Now.ToString() + "] 경험치 5배 이벤트 시작");
                Packet("<exp_event> 5 </exp_event>");
            }
        }

        private void shipTime()
        {

        }

        private void ListBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void RadioButton_4_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void ListBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Right)
            {
                if (listBox1.SelectedIndex >= 0)
                {
                    EventHandler eh = new EventHandler(MenuClick);
                    MenuItem[] ami = {
                        new MenuItem("감옥",eh),
                        new MenuItem("석방",eh),
                        new MenuItem("강퇴",eh)
                    };
                    ContextMenu = new System.Windows.Forms.ContextMenu(ami);
                }
            }
        }

        private void MenuClick(object obj, EventArgs ea)
        {
            MenuItem mI = (MenuItem)obj;
            String str = mI.Text;
            if (listBox1.SelectedIndex >= 0)
            {
                if (str == "감옥")
                {
                    string name = UserList[listBox1.SelectedIndex].UserName;
                    Packet("<prison>" + name + "</prison>");
                    listBox2.Items.Add("(" + DateTime.Now.ToString() + ") " + name + " 감옥");
                    textBox1.Text = "";
                }
                if (str == "석방")
                {
                    string name = UserList[listBox1.SelectedIndex].UserName;
                    Packet("<emancipation>" + name + "</emancipation>");
                    listBox2.Items.Add("(" + DateTime.Now.ToString() + ") " + name + " 석방");
                    textBox1.Text = "";
                }
                if (str == "강퇴")
                {
                    string name = UserList[listBox1.SelectedIndex].UserName;
                    if(textBox1.Text == "")
                        Packet("<ki>" + name + ",강퇴 당하셨습니다.,</ki>");
                    else
                        Packet("<ki>" + name + "," + textBox1.Text + ",</ki>");
                    Packet("<chat>" + name + "님이 강퇴 당하셨습니다." + "</chat>");
                    listBox2.Items.Add("(" + DateTime.Now.ToString() + ") " + name + " 강퇴");
                    textBox1.Text = "";
                }
            }
        }
    }
}
