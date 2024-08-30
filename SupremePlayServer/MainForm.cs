using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace SupremePlayServer
{
    public partial class mainForm : Form
    {
        public bool isTest = false;
        public Systemdata sd;
        public Dictionary<int, List<UserThread>> MapUser2;
        public Dictionary<string, UserThread> UserByNameDict;

        public System_DB systemDB;
        public int count_down = 0; // 리붓 카운트 다운
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

        public double exe_event = 0;
        public double drop_event = 0;

        public bool weekendEventTriggered = false;
        public int max_user_name = 15; // 전체 인원 제한
        public string version = Properties.Resources.VERSION;

        public List<string> settingList;

        Random random;
        public List<string> print_chat_tag; // 서버에 남길 채팅 내용 태그

        private int secTime = 1000;
        private int randomMsgIdx = -1;
        public mainForm()
        {
            isTest = true; // 테스트용 서버인가?
            random = new Random();
            sd = new Systemdata();
            systemDB = sd.system_db; // new System_DB();
            if (isTest)
            {
                systemDB.DBInfo += "Server=127.0.0.1;";
            }
            else
            {
                systemDB.DBInfo += "Server=database-1.c3c2a4qqcid0.ap-northeast-2.rds.amazonaws.com;";
            }

            sd.mainForm = this;
            systemDB.mainForm = this;
            print_chat_tag = new List<string>();
            make_chat_tag(print_chat_tag);

            InitializeComponent();
            radioButton_1.Select(); // 처음에 공지로 미리 선택됨

            // 타이머 생성 및 시작
            System.Windows.Forms.Timer timer2 = new System.Windows.Forms.Timer();
            timer2.Interval = secTime; // 몹 리젠 시간
            timer2.Tick += new EventHandler(timer_tick);
            timer2.Start();

            // 랜덤 메시지용 타이머
            System.Windows.Forms.Timer timer3 = new System.Windows.Forms.Timer();
            timer3.Interval = secTime * 60 * 10;
            timer3.Tick += new EventHandler(random_server_msg);
            timer3.Start();

            // 배 목적지 타이머
            System.Windows.Forms.Timer timer4 = new System.Windows.Forms.Timer();
            timer4.Interval = secTime * 60 * 1;
            timer4.Tick += new EventHandler(change_ship_target);
            timer4.Start();

            // 서버 시작할 때 몹 데이터 정리
            write_log("------------------------------");
            write_log("서버 시작");
            write_log("몬스터 데이터 삭제");
            if (isTest) write_log("테스트 서버 시작");
            now_ship_target(); // 선착장 확인

            // 기본 셋팅
            drop_event_num.Text = "0";
            exp_event_num.Text = "0";

            // 버전 확인
            try
            {
                string dir = "./";
                FileInfo fileInfo = new FileInfo(dir + "version.txt");
                if (!fileInfo.Exists)
                {
                    using (StreamWriter verFile = new StreamWriter(@dir + "version.txt", true))
                    {
                        verFile.WriteLine(version);
                        verFile.Close();
                    }
                }

                using (StreamReader verFile = new StreamReader(@dir + "version.txt", true))
                {
                    version = verFile.ReadLine();
                    verFile.Close();
                }
                write_log("현재 버전 : " + version);
            }
            catch (Exception e)
            {
                write_log(e.ToString());
            }

            // 서버 켜기전 셋팅 값 불러오기
            try
            {
                string dir = "./";
                FileInfo fileInfo = new FileInfo(dir + "setting.dat");
                if (!fileInfo.Exists) // 파일이 존재 안한다면
                {
                    using (StreamWriter setFile = new StreamWriter(@dir + "setting.dat", true))
                    {
                        setFile.WriteLine(Properties.Resources.DROP_SET_NAME + " " + drop_event_num.Text);
                        setFile.WriteLine(Properties.Resources.EXE_SET_NAME + " " + exp_event_num.Text);
                        setFile.Close();
                    }
                }

                using (StreamReader setFile = new StreamReader(@dir + "setting.dat", true))
                {
                    string line = "";
                    while ((line = setFile.ReadLine()) != null)
                    {
                        string[] data = line.Split(' ');
                        string setName = data[0];
                        string val = data[1];

                        if (setName.Equals(Properties.Resources.EXE_SET_NAME))
                        {
                            exp_event_num.Text = val;
                            double.TryParse(val, out exe_event);
                        }

                        else if (setName.Equals(Properties.Resources.DROP_SET_NAME))
                        {
                            drop_event_num.Text = val;
                            double.TryParse(val, out drop_event);
                        }

                        write_log(setName + " : " + val);
                    }
                    setFile.Close();
                }
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
            tag_list.Add("party_message");
            tag_list.Add("whispers");
            tag_list.Add("map_chat");
        }

        public string abuse_filtering(string message)
        {
            // 욕설 리스트 정의
            List<string> abusiveWords = new List<string> {
                "씨발", "병신", "새끼", "섹스", "sex",
                "좆", "존나", "개새끼", "죽일놈", "미친놈",
                "꺼져", "엿먹어",
                "ㅅㅂ", "ㅄ", "ㅗ", "ㅆㅂ",
                "자지", "보지", "좆", "봊",
            }; // 실제 필터링할 단어들로 교체

            // 정규식을 통해 욕설을 "앙"으로 대체
            foreach (var word in abusiveWords)
            {
                string pattern = Regex.Escape(word); // 단어 자체에 정규식 특수 문자가 포함될 수 있으므로 이스케이프 처리
                message = Regex.Replace(message, pattern, "욜", RegexOptions.IgnoreCase);
            }

            return message;
        }

        void timer_tick(object sender, EventArgs e)
        {
            try
            {
                DateTime nowTime = DateTime.Now;
                string dayOfWeek = nowTime.DayOfWeek.ToString(); // 요일

                string t = nowTime.ToString("HH:mm:ss");
                label3.Text = "현재 시간 : " + t;

                // 초당 몬스터 db에서 체력 0인 몹의 리젠 시간을 줄인다.
                List<string> list = sd.respawnMonster2();
                if (list.Count > 0)
                {
                    foreach (var s in list)
                    {
                        String[] data = s.Split(',');
                        Map_Packet("respawn", s, int.Parse(data[0]));
                    }
                }

                if (nowTime.Minute == 0 && nowTime.Second == 0)
                {
                    Packet("chat", "맵의 모든 아이템들이 삭제 됩니다.");
                    sd.DelAllItem();
                }

                if (nowTime.Hour == 0) // 매 정각마다 확인
                    weekendEventTriggered = false;

                // 토요일 또는 일요일이면 이벤트를 발생시킴
                if ((dayOfWeek == "Saturday" || dayOfWeek == "Sunday") && !weekendEventTriggered)
                {
                    // 주말 이벤트가 아직 트리거되지 않은 경우
                    Packet("chat", "주말 이벤트가 시작됩니다!");
                    weekendEventTriggered = true; // 이벤트가 트리거되었음을 기록

                    switch (dayOfWeek)
                    {
                        case "Saturday":
                            exe_event_send(2);
                            drop_event_send(1.2);
                            break;
                        case "Sunday":
                            exe_event_send(3);
                            drop_event_send(1.3);
                            break;
                    }
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
                Packet("chat", count_down + "초 후 리붓합니다. 안전한 곳으로 이동하시길 바랍니다.");
                if (count_down <= 0) // 전체 강퇴
                {
                    Packet("over", "모두,비바람이 휘몰아치고 있습니다. 잠시만 기다려 주세요.");
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

        void random_server_msg(object sender, EventArgs e) // 랜덤 도움 메시지
        {
            try
            {
                if (UserByNameDict.Count == 0) return;

                int i = random.Next(0, sd.random_server_msg.Count);
                while (i == randomMsgIdx)
                {
                    i = random.Next(0, sd.random_server_msg.Count);
                }
                string msg = sd.random_server_msg[i];
                randomMsgIdx = i;
                Packet("chat2", "[도움]" + msg);
            }
            catch
            {
                write_log(e.ToString());
            }
        }

        void change_ship_target(object sender, EventArgs e) // 선착장 배 목적지 타이머
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
                    userthread.mainForm = this;
                    userthread.StartClient(client);
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
        public void Packet(string tag, string body, String userCode = "")
        {
            foreach (var user in UserByNameDict.Values)
            {
                // 유효하지 않은 유저는 삭제
                if (user.userCode.Equals("*null*"))
                {
                    removethread(user);
                    continue;
                }

                if (user.userCode.Equals(userCode)) continue;

                try
                {
                    user.SendMessageWithTag(tag, body);
                }
                catch (Exception e) // 팅긴걸로 판단
                {
                    removethread(user);
                }
            }

            process_chat_tag(tag, body);
        }

        public string process_chat_tag(string tag, string body)
        {
            if (!print_chat_tag.Contains(tag)) return body;

            body = abuse_filtering(body);
            write_log(body);
            autoListBoxScroll();
            return body;
        }

        // 해당 맵의 유저들에게만 전송하는 패킷
        public void Map_Packet(string tag, string body, int map_id, String userCode = "")
        {
            if (!MapUser2.ContainsKey(map_id)) return;

            foreach (var user in MapUser2[map_id])
            {
                if (user.userCode.Equals("*null*"))
                {
                    removethread(user);
                    continue;
                }

                if (user.userCode.Equals(userCode)) continue;

                try
                {
                    user.SendMessageWithTag(tag, body);
                }
                catch (Exception e) // 팅긴걸로 판단
                {
                    removethread(user);
                }
            }
        }

        public UserThread findMember(string name)
        {
            if (!UserByNameDict.ContainsKey(name)) return null;
            return UserByNameDict[name];
        }

        // 유저의 맵 이름을 변경한다.
        public void editUserMap(UserThread userThread, int new_id)
        {
            int old_id = userThread.lastMapId;
            int flag_id = 0;
            if (!MapUser2.ContainsKey(new_id)) // 해당 맵에 아무도 없었다면?
            {
                MapUser2.Add(new_id, new List<UserThread>());
            }

            if (MapUser2[new_id].Count == 0) flag_id = 1;
            userThread.SendMessageWithTag("map_player", flag_id.ToString());

            MapUser2[new_id].Add(userThread);
            removeMapUser(old_id, userThread); // 이전에 있었던 리스트에서 제거함
            PlayerCount();
        }

        public void removeMapUser(int map_i, UserThread userThread)
        {
            try
            {
                if (!MapUser2.ContainsKey(map_i)) return;
                if (!MapUser2[map_i].Contains(userThread)) return;
                MapUser2[map_i].Remove(userThread);

                if (MapUser2[map_i].Count <= 0) return;
                UserThread firstUser = MapUser2[map_i][0];
                if (!firstUser.thread.IsAlive) return;
                firstUser.SendMessageWithTag("map_player", "1");
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
                int map_i = userthread.lastMapId;
                removeMapUser(map_i, userthread);
                if (!string.IsNullOrEmpty(userthread.userName))
                {
                    UserByNameDict.Remove(userthread.userName);
                    write_log(userthread.userName + " 종료");
                    Packet("chat1", "(알림): '" + userthread.userName + "'님께서 종료하셨습니다.");
                }

                PlayerCount();
                Packet("9", userthread.userCode);
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
                foreach (var user in UserByNameDict.Values)
                {
                    if (user.userId == null) continue;
                    if (user.userId.Equals(id)) check = true;
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

        private readonly object _userLock = new object();

        public void PlayerCount()
        {
            CheckForIllegalCrossThreadCalls = false;

            lock (_userLock)
            {
                // UserByNameDict.Count를 가져오는 동안 스레드 충돌을 방지
                var userCount = UserByNameDict.Count;

                // UI 업데이트를 UI 스레드에서만 실행하도록 BeginInvoke 사용
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => UpdateUI(userCount)));
                }
                else
                {
                    UpdateUI(userCount);
                }
            }
        }

        private void UpdateUI(int userCount)
        {
            toolStripStatusLabel2.Text = "접속자 수 : " + userCount;

            listBox1.BeginUpdate();
            listBox1.Items.Clear();

            lock (_userLock)
            {
                foreach (var user in UserByNameDict.Values)  // .ToList()로 복사본을 만듦
                {
                    listBox1.Items.Add(user.userName + ": " + user.mapName);
                }
            }

            listBox1.EndUpdate();
        }


        public void addUserToList(UserThread userthread)
        {

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

        private void handle_prison()
        {
            if (listBox1.SelectedIndex < 0) return;

            string name = listBox1.SelectedItem.ToString().Split(':')[0];
            Packet("prison", name);
            write_log(name + " 감옥");
            textBox1.Text = "";
        }

        private void handle_emancipation()
        {
            if (listBox1.SelectedIndex < 0) return;

            string name = listBox1.SelectedItem.ToString().Split(':')[0];
            Packet("emancipation", name);
            write_log(name + " 석방");
            textBox1.Text = "";
        }
        private void handle_kick()
        {
            if (listBox1.SelectedIndex < 0) return;

            string name = listBox1.SelectedItem.ToString().Split(':')[0];
            string msg = textBox1.Text.Equals("") ? "강퇴 당하셨습니다." : textBox1.Text;
            Packet("ki", name + "," + msg);
            Packet("chat", name + "님이 강퇴 당하셨습니다.");
            write_log(name + " 강퇴");
            textBox1.Text = "";
        }

        // 공지 보내기
        private void button1_Click(object sender, EventArgs e)
        {
            if (radioButton_1.Checked) // 공지
            {
                if (!textBox1.Text.Equals(""))
                {
                    Packet("chat", "(공지) : " + textBox1.Text);
                    textBox1.Text = "";
                }
            }
            else if (radioButton_2.Checked) // 감옥
            {
                handle_prison();
            }
            else if (radioButton_3.Checked) // 석방
            {
                handle_emancipation();
            }
            else if (radioButton_4.Checked) // 유저 강퇴
            {
                handle_kick();
            }
            else if (radioButton_5.Checked) // 모두 강퇴
            {
                Packet("ki", "모두," + textBox1.Text);
                write_log("모두 강퇴 : " + textBox1.Text);
                textBox1.Text = "";
            }
            // 자동 스크롤
            autoListBoxScroll();
        }

        public void autoListBoxScroll()
        {
            // 자동 스크롤
            int visibleItems = listBox2.ClientSize.Height / listBox2.ItemHeight;

            if (Math.Abs(listBox2.TopIndex - listBox2.Items.Count + visibleItems) <= 3)
                listBox2.TopIndex = Math.Max(listBox2.Items.Count - 1, 0);
            return;

            //listBox2.TopIndex = Math.Max(listBox2.Items.Count - visibleItems + 1, 0);
        }

        private void message_keyDown(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                this.button1_Click(sender, e);
            }
        }

        private void HandleEventSend(object sender, KeyPressEventArgs e, string eventType, TextBox eventNumTextBox, string logMessage, string settingResourceName, ref double eventValue)
        {
            if (e.KeyChar == '\r')
            {
                if (double.TryParse(eventNumTextBox.Text, out double n))
                {
                    Packet(eventType, n.ToString());
                    write_log($"{logMessage} {n}배 이벤트 시작");
                    eventValue = n;

                    autoListBoxScroll();
                    UpdateSettingFile(settingResourceName, n);
                }
            }
        }

        private void HandleEventSend(string eventType, TextBox eventNumTextBox, string logMessage, string settingResourceName, ref double eventValue)
        {
            if (double.TryParse(eventNumTextBox.Text, out double n))
            {
                Packet(eventType, n.ToString());
                write_log($"{logMessage} {n}배 이벤트 시작");
                eventValue = n;

                autoListBoxScroll();
                UpdateSettingFile(settingResourceName, n);
            }
        }

        private void UpdateSettingFile(string settingResourceName, double eventValue)
        {
            string dir = "./";
            try
            {
                string buf;
                using (StreamReader setFile = new StreamReader(@dir + "setting.dat", true))
                {
                    buf = setFile.ReadToEnd();
                }

                int offset = buf.IndexOf(settingResourceName);
                if (offset >= 0)
                {
                    string line = buf.Substring(offset, buf.IndexOf('\n', offset) - offset);
                    buf = buf.Replace(line, $"{settingResourceName} {eventValue}");
                }

                using (StreamWriter setFile = new StreamWriter(@dir + "setting.dat", false))
                {
                    setFile.Write(buf);
                }
            }
            catch (Exception exc)
            {
                write_log(exc.ToString());
            }
        }

        private void exe_event_send(object sender, KeyPressEventArgs e)
        {
            HandleEventSend(sender, e, "exp_event", exp_event_num, "경험치", Properties.Resources.EXE_SET_NAME, ref exe_event);
        }
        public void exe_event_send(double num)
        {
            exp_event_num.Text = num.ToString();
            exe_event = num;
            HandleEventSend("exp_event", exp_event_num, "경험치", Properties.Resources.EXE_SET_NAME, ref exe_event);
        }

        private void drop_event_send(object sender, KeyPressEventArgs e)
        {
            HandleEventSend(sender, e, "drop_event", drop_event_num, "드랍율", Properties.Resources.DROP_SET_NAME, ref drop_event);
        }
        public void drop_event_send(double num)
        {
            drop_event_num.Text = num.ToString();
            drop_event = num;
            HandleEventSend("drop_event", drop_event_num, "드랍율", Properties.Resources.DROP_SET_NAME, ref drop_event);
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
                    handle_prison();
                }
                if (str == "석방")
                {
                    handle_emancipation();
                }
                if (str == "강퇴")
                {
                    handle_kick();
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
                        listBox2.Items.Add("[" + t + "] " + s);
                        logfile.WriteLine("[" + t + "] " + s);
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
            string msg = sw_num + "," + state;
            if (map_id > 0)
                Map_Packet("switches", msg, map_id);
            else
                Packet("switches", msg);
        }

        public void monster_cooltime_reset(int map_id, int mon_id = 0)
        {
            if (!sd.monster_data.ContainsKey(map_id)) return;

            foreach (var v in sd.monster_data[map_id].Values)
            {
                if (v.respawn <= 0) continue;
                if (mon_id == 0) v.respawn = 2;
                else if (v.id == mon_id)
                {
                    v.respawn = 2;
                    return;
                }
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

                autoListBoxScroll();
            }
            catch
            {

            }
        }
    }
}
