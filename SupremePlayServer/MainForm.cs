using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using Google.Protobuf.Collections;
using System.Diagnostics;

namespace SupremePlayServer
{
    public partial class MainForm : Form
    {
        public systemdata sd;
        public List<UserThread> UserList;
        public Dictionary<int, List<UserThread>> MapUser2;
        public System_DB system_db;
        public int count_down = 0; // 리붓 카운트 다운
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        public int exe_event = 0;
        public int max_user_name = 10; // 전체 인원 제한
        
        public MainForm()
        {
            system_db = new System_DB();
            sd = new systemdata();
            InitializeComponent();
            radioButton1.Select(); // 경험치 이벤트 없음
            radioButton_1.Select(); // 처음에 공지로 미리 선택됨

            // 타이머 생성 및 시작
            System.Windows.Forms.Timer timer2 = new System.Windows.Forms.Timer();
            timer2.Interval = 1000; // 몹 리젠 시간
            timer2.Tick += new EventHandler(timer_tick);
            timer2.Start();

            // 서버 시작할 때 몹 데이터 정리
            write_log("------------------------------");
            write_log("서버 시작");
            write_log("몬스터 데이터 삭제");
        }

        void timer_tick(object sender, EventArgs e)
        {
            try
            {
                string t = DateTime.Now.ToString("HH:mm:ss");
                label3.Text = "현재 시간 : " + t;
                // 초당 몬스터 db에서 체력 0인 몹의 리젠 시간을 줄인다.
                List<string> list = sd.respawnMonster2();
                if(list.Count > 0)
                {
                    foreach (var s in list)
                    {
                        Packet("<respawn>" + s + "</respawn>");
                    }
                }

                if(t.Contains(":00:00"))
                {
                    write_log("맵의 모든 아이템 삭제");
                    Packet("<chat>맵의 모든 아이템들이 삭제 됩니다.</chat>");

                    sd.DelAllItem();
                }
            }
            catch
            {
                write_log(e.ToString());
            }
        }

        void timer_tick2(object sender, EventArgs e) // 리붓용 타이머 이벤트
        {
            try
            {
                count_down--;
                textBox2.Text = count_down.ToString();
                Packet("<chat>" + count_down + "초 후 리붓합니다. 안전한 곳으로 이동하시길 바랍니다.</chat>");
                write_log("리붓 " + count_down + "초 전");
                if (count_down <= 0) // 전체 강퇴
                {
                    Packet("<ki>모두,비바람이 휘몰아치고 있습니다. 잠시만 기다려 주세요.,</ki>");
                    write_log("리붓 완료");
                    textBox2.Text = "";
                    timer.Enabled = false;
                    timer.Stop();
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
            List<String> a = new List<string> { "0" };

            MapUser2 = new Dictionary<int, List<UserThread>>();
            
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

                    if (!MapUser2.ContainsKey(0))
                        MapUser2.Add(0, new List<UserThread>());
                    MapUser2[0].Add(userthread);
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

            if (data.Contains("<chat1>"))
            {
                string[] word = splitTag("chat1", data).Split(',');
                write_log(word[0]);
                int visibleItems = listBox2.ClientSize.Height / listBox2.ItemHeight;
                listBox2.TopIndex = Math.Max(listBox2.Items.Count - visibleItems + 1, 0);   
            }
            if (data.Contains("<chat>"))
            {
                string[] word = splitTag("chat", data).Split(',');
                write_log(word[0]);
                int visibleItems = listBox2.ClientSize.Height / listBox2.ItemHeight;
                listBox2.TopIndex = Math.Max(listBox2.Items.Count - visibleItems + 1, 0);
            }
        }

        // 해당 맵의 유저들에게만 전송하는 패킷
        public void Map_Packet(String data, int map_id, String userCode = "")
        {
            for (int i = 0; i < MapUser2[map_id].Count; i++)
            {
                if (!MapUser2[map_id][i].UserCode.Equals("*null*"))
                {
                    if (MapUser2[map_id][i].UserCode.Equals(userCode))
                        continue;
                    try
                    {
                        //MessageBox.Show(data);
                        MapUser2[map_id][i].SW.WriteLine(data); // 메시지 보내기
                        MapUser2[map_id][i].SW.Flush();
                    }
                    catch (Exception e) // 팅긴걸로 판단
                    {
                        removethread(MapUser2[map_id][i]);
                    }
                }
                // 유효하지 않은 유저는 삭제
                else
                {
                    removethread(MapUser2[map_id][i]);
                }
            }
        }

        public void removeMapUser(int map_i, UserThread userThread)
        {
            MapUser2[map_i].Remove(userThread);   
            try
            {
                if (MapUser2[map_i].Count > 0)
                {
                    if(MapUser2[map_i][0].thread.IsAlive)
                    {
                        MapUser2[map_i][0].SW.WriteLine("<map_player>1</map_player>"); // 메시지 보내기
                        MapUser2[map_i][0].SW.Flush();
                    }
                }
            }
            catch (Exception e)
            {
                write_log(e.ToString());
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
                    int map_i = userthread.last_map_id;
                    removeMapUser(map_i, userthread);

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
                            write_log(userthread.UserName + " 종료");
                            Packet("<chat1>(알림): '" + userthread.UserName + "'님께서 종료하셨습니다.</chat1>");
                            Packet("<9>" + userthread.UserCode + "</9>");
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
                MessageBox.Show(e.ToString());
            }

            return check;
        }

        private void FormClose(object sender, FormClosedEventArgs e)
        {
            write_log("서버 종료");
            write_log("------------------------------");
            Application.ExitThread();
            Environment.Exit(0);
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        public void PlayerCount()
        {
            CheckForIllegalCrossThreadCalls = false;
            toolStripStatusLabel2.Text = "접속자 수 : " + UserList.Count;

            listBox1.Items.Clear();
            for (int i = 0; i < UserList.Count; i++)
            {
                listBox1.Items.Add(UserList[i].UserName + "(" + UserList[i].UserId + ")" + ": " + UserList[i].map_name);
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
                    write_log(name + " 감옥");
                    textBox1.Text = "";
                }
            }
            else if (radioButton_3.Checked) // 석방
            {
                if (listBox1.SelectedIndex >= 0)
                {
                    string name = UserList[listBox1.SelectedIndex].UserName;
                    Packet("<emancipation>" + name + "</emancipation>");
                    write_log(name + " 석방");
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
                    write_log(name + " 강퇴");
                    textBox1.Text = "";
                }
            }
            else if (radioButton_5.Checked) // 모두 강퇴
            {
                Packet("<ki>모두," + textBox1.Text + ",</ki>");
                write_log("모두 강퇴 : " + textBox1.Text);
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

        private void exe_event_send(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                int n = -1;
                if(int.TryParse(exp_event_num.Text, out n))
                {
                    Packet("<exp_event> " + exp_event_num.Text + " </exp_event>");
                    write_log("경험치 " + n + "배 이벤트 시작");
                    exe_event = n;
                }
            }
        }

        private void RadioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked == true && UserList != null)
            {
                write_log("경험치 이벤트 종료");
                Packet("<exp_event> 0 </exp_event>");
            }
        }

        private void RadioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked == true && UserList != null)
            {
                write_log("경험치 2배 이벤트 시작");
                Packet("<exp_event> 2 </exp_event>");
            }
        }

        private void RadioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked == true && UserList != null)
            {
                write_log("경험치 3배 이벤트 시작");
                Packet("<exp_event> 3 </exp_event>");
            }
        }

        private void RadioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton4.Checked == true && UserList != null)
            {
                write_log("경험치 5배 이벤트 시작");
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
                    write_log(name + " 감옥");
                    textBox1.Text = "";
                }
                if (str == "석방")
                {
                    string name = UserList[listBox1.SelectedIndex].UserName;
                    Packet("<emancipation>" + name + "</emancipation>");
                    write_log(name + " 석방");
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
                    write_log(name + " 강퇴");
                    textBox1.Text = "";
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int n = -1;
            if (int.TryParse(textBox2.Text, out n))
            {
                timer = new System.Windows.Forms.Timer();
                timer.Interval = 1000; // 몹 리젠 시간
                count_down = n;
                timer.Enabled = true;
                timer.Tick += timer_tick2;
                timer.Start();
            }
            else
            {
                write_log("(입력 오류)숫자를 입력하세요");
            }
        }

        public void write_log(string s)
        {
            // 지금까지의 로그를 저장하기
            try
            {
                string t = DateTime.Now.ToShortDateString();
                string dir = "./LogServer/";
                if (!Directory.Exists(@dir))
                    Directory.CreateDirectory(@dir);
                using (StreamWriter logfile = new StreamWriter(@dir + "(" + t + ")Log.txt", true))
                {
                    t = DateTime.Now.ToString();
                    if (s != null && s.Length > 0)
                    {
                        listBox2.Items.Add("[" + t + "]" + s);
                        logfile.WriteLine("[" + t + "]" + s);
                    }
                }
            }
            catch
            {

            }
        }

        // 유저별 데이터 로그 저장
        public void write_log_user(string name, string data)
        {
            // 지금까지의 로그를 저장하기
            try
            {
                string t = DateTime.Now.ToShortDateString();
                string dir = "./LogUser/" + name + "/";
                if (!Directory.Exists(@dir))
                    Directory.CreateDirectory(@dir);
                using (StreamWriter logfile = new StreamWriter(@dir + "(" + t + ")" + name + "Log.txt", true))
                {
                    t = DateTime.Now.ToString();
                    if (data != null && data.Length > 0)
                    {
                        logfile.WriteLine("[" + t + "]" + data);
                    }
                }
            }
            catch
            {

            }
        }
    }
}
