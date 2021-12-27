using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace SupremePlayServer
{
    public partial class MainForm : Form
    {
        public Systemdata sd;
        public List<UserThread> UserList;
        public Dictionary<int, List<UserThread>> MapUser2;
        public Dictionary<string, UserThread> UserByNameDict;

        public System_DB system_db;
        public int count_down = 0; // 리붓 카운트 다운
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        public int exe_event = 0;
        public double drop_event = 0;
        public int max_user_name = 10; // 전체 인원 제한
        public string version = Properties.Resources.VERSION;
        Random random;
        List<string> print_chat_tag; // 서버에 남길 채팅 내용 태그

        public MainForm()
        {
            random = new Random();
            sd = new Systemdata();
            system_db = sd.system_db; // new System_DB();
            sd.mainForm = this;
            system_db.mainform = this;
            print_chat_tag = new List<string>();
            make_chat_tag(print_chat_tag);

            InitializeComponent();
            radioButton_1.Select(); // 처음에 공지로 미리 선택됨

            // 타이머 생성 및 시작
            System.Windows.Forms.Timer timer2 = new System.Windows.Forms.Timer();
            timer2.Interval = 1000; // 몹 리젠 시간
            timer2.Tick += new EventHandler(timer_tick);
            timer2.Start();

            // 랜덤 메시지용 타이머
            System.Windows.Forms.Timer timer3 = new System.Windows.Forms.Timer();
            timer3.Interval = 1000 * 60 * 5; 
            timer3.Tick += new EventHandler(random_server_msg);
            timer3.Start();

            // 배 목적지 타이머
            System.Windows.Forms.Timer timer4 = new System.Windows.Forms.Timer();
            timer4.Interval = 1000 * 60 * 1;
            timer4.Tick += new EventHandler(change_ship_target);
            timer4.Start();

            // 서버 시작할 때 몹 데이터 정리
            write_log("------------------------------");
            write_log("서버 시작");
            write_log("몬스터 데이터 삭제");
            now_ship_target();
            try
            {
                string dir = "./";
                FileInfo fileInfo = new FileInfo(dir + "version.txt");
                if (!fileInfo.Exists)
                {
                    using (StreamWriter verFile = new StreamWriter(@dir + "version.txt", true))
                    {
                        verFile.WriteLine(version);
                    }
                }
                using (StreamReader verFile = new StreamReader(@dir + "version.txt", true))
                {
                    version = verFile.ReadLine();
                    Console.WriteLine(version);
                }
                write_log("현재 버전 : " + version);
            }
            catch (Exception e)
            {
                write_log(e.ToString());
            }
        }

        public void make_chat_tag(List<string> tag_list)
        {
            tag_list.Add("chat");
            tag_list.Add("chat1");
            tag_list.Add("chat2");
            tag_list.Add("partymessage");
            tag_list.Add("whispers");
        }

        void timer_tick(object sender, EventArgs e)
        {
            try
            {
                string t = DateTime.Now.ToString("HH:mm:ss");
                label3.Text = "현재 시간 : " + t;
                // 초당 몬스터 db에서 체력 0인 몹의 리젠 시간을 줄인다.
                List<string> list = sd.respawnMonster2();
                if (list.Count > 0)
                {
                    foreach (var s in list)
                    {
                        String[] data = s.Split(',');
                        Map_Packet("<respawn>" + s + "</respawn>", int.Parse(data[0]));
                    }
                }

                if (t.Contains(":00:00"))
                {
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
                write_log(e.ToString());
            }
        }

        void random_server_msg(object sender, EventArgs e) // 리붓용 타이머 이벤트
        {
            try
            {
                if (UserList.Count == 0) return;
                int i = random.Next(0, sd.random_server_msg.Count);
                string msg = sd.random_server_msg[i];
                Packet("<chat2>[도움]"+ msg + "</chat2>");
            }
            catch
            {
                write_log(e.ToString());
            }
        }

        void change_ship_target(object sender, EventArgs e) // 리붓용 타이머 이벤트
        {
            try
            {
                now_ship_target();
            }
            catch
            {
                write_log(e.ToString());
            }
        }

        public int now_ship_target()
        {
            int t = int.Parse(DateTime.Now.ToString("mm"));
            int val = -1;
            if ((0 < t && t < 10) || (20 < t && t < 30) || (40 < t && t < 50))
            {
                ship_target_name.Text = "일본";
                val = 0;
            }
            else if ((10 < t && t < 20) || (30 < t && t < 40) || (50 < t))
            {
                ship_target_name.Text = "고균도";
                val = 1;
            }
            return val;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Initialize
            UserList = new List<UserThread>();
            List<String> a = new List<string> { "0" };
            MapUser2 = new Dictionary<int, List<UserThread>>();
            UserByNameDict = new Dictionary<string, UserThread>();

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
                write_log(e.ToString());
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

            foreach(string tag in print_chat_tag)
            {
                if (data.Contains("<" + tag + ">"))
                {
                    string word = splitTag(tag, data);
                    write_log(word);
                    int visibleItems = listBox2.ClientSize.Height / listBox2.ItemHeight;
                    listBox2.TopIndex = Math.Max(listBox2.Items.Count - visibleItems + 1, 0);
                    break;
                }
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
            try
            {
                if (MapUser2.ContainsKey(map_i) && MapUser2[map_i].Contains(userThread))
                {
                    MapUser2[map_i].Remove(userThread);
                }
                else
                {
                    return;
                }

                if (MapUser2[map_i].Count > 0)
                {
                    if (MapUser2[map_i][0].thread.IsAlive)
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
                if (userthread == null) return;
                // 접속이 되지 않은 유저 삭제 : 중간에 팅긴 유저에 대한 처리
                if (!userthread.client.Connected) // 접속이 끊겼는데 접속 되어 있다고 처리되서 계속 오류나고 있음
                {
                    int map_i = userthread.last_map_id;
                    removeMapUser(map_i, userthread);
                    UserList.Remove(userthread);
                    PlayerCount();
                    if (!string.IsNullOrEmpty(userthread.UserName))
                    {
                        UserByNameDict.Remove(userthread.UserName);
                        write_log(userthread.UserName + " 종료");
                        Packet("<chat1>(알림): '" + userthread.UserName + "'님께서 종료하셨습니다.</chat1>");
                        Packet("<9>" + userthread.UserCode + "</9>");
                    }
                }
            }
            catch (Exception e)
            {
                write_log(e.ToString());
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
                write_log(e.ToString());
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
                if (int.TryParse(exp_event_num.Text, out n))
                {
                    Packet("<exp_event> " + n + " </exp_event>");
                    write_log("경험치 " + n + "배 이벤트 시작");
                    exe_event = n;

                    int visibleItems = listBox2.ClientSize.Height / listBox2.ItemHeight;
                    listBox2.TopIndex = Math.Max(listBox2.Items.Count - visibleItems + 1, 0);
                }
            }
        }

        private void drop_event_send(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                double n = -1;
                if (double.TryParse(drop_event_num.Text, out n))
                {
                    Packet("<drop_event> " + n + " </drop_event>");
                    write_log("드랍율 " + n + "배 이벤트 시작");
                    drop_event = n;

                    int visibleItems = listBox2.ClientSize.Height / listBox2.ItemHeight;
                    listBox2.TopIndex = Math.Max(listBox2.Items.Count - visibleItems + 1, 0);
                }
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
            if (e.Button == MouseButtons.Right)
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
                    if (textBox1.Text == "")
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
                if (timer != null && timer.Enabled) return;
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

        // 맵의 유저들에게 스위치 공유
        public void switch_send(string sw_num, string state, int map_id = 0)
        {
            if (map_id > 0)
                Map_Packet("<switches>" + sw_num + "," + state + "</switches>", map_id);
            else
                Packet("<switches>" + sw_num + "," + state + "</switches>");
        }

        public void monster_cooltime_reset(int map_id, int mon_id = 0)
        {
            foreach (var v in sd.monster_data[map_id].Values)
            {
                if (mon_id != 0)
                {
                    if (v.id == mon_id && v.respawn > 0)
                        v.respawn = 10;
                }
                else if (v.respawn > 0)
                    v.respawn = 120;
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
                int visibleItems = listBox2.ClientSize.Height / listBox2.ItemHeight;
                listBox2.TopIndex = Math.Max(listBox2.Items.Count - visibleItems + 1, 0);
            }
            catch
            {

            }
        }
    }
}
