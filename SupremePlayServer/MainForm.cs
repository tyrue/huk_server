using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace SupremePlayServer
{
    public partial class mainForm : Form
    {
        public bool isTest = false;
        public Systemdata sd;
        public ConcurrentDictionary<int, List<UserThread>> MapUser2;
        public ConcurrentDictionary<string, UserThread> UserByNameDict;

        public System_DB systemDB;
        public int count_down = 0; // 리붓 카운트 다운
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

        public double exe_event = 0;
        public double drop_event = 0;

        public int check_ship_target = -1;
        public int reboot_min = -1;
        public bool weekendEventTriggered = false;
        public int max_user_name = 15; // 전체 인원 제한
        public string version = Properties.Resources.VERSION;

        public List<string> settingList;

        Random random;
        private HashSet<string> bannedIPs = new HashSet<string>(); // 차단된 IP 목록
        public List<string> print_chat_tag = new List<string>(); // 서버에 남길 채팅 내용 태그

        private int secTime = 1000;
        private int randomMsgIdx = -1;
        private SemaphoreSlim _userLock = new SemaphoreSlim(1, 1); // 비동기 작업을 위한 세마포어

        public mainForm()
        {
            InitializeComponent();
            InitializeServer();
            InitializeTimers();
            LoadServerSettings();
            LoadServerVersion();
            LoadEventSettings();
            LoadIPBanSettings();
        }

        private void InitializeServer()
        {
            isTest = true;
            random = new Random();
            
            systemDB = new System_DB(this);
            systemDB.DBInfo += isTest ? "Server=localhost;" : "Server=baram.c3c2a4qqcid0.ap-northeast-2.rds.amazonaws.com;";
            sd = new Systemdata(this);

            make_chat_tag(print_chat_tag);

            radioButton_1.Select();
            write_log("------------------------------");
            write_log("서버 시작");
            write_log("몬스터 데이터 삭제");
            if (isTest) write_log("테스트 서버 시작");

            now_ship_target();
            drop_event_num.Text = "0";
            exp_event_num.Text = "0";
        }

        private void InitializeTimers()
        {
            CreateAndStartTimer(secTime, timer_tick);
            CreateAndStartTimer(secTime * 60 * 10, random_server_msg);
            CreateAndStartTimer(secTime * 10 , change_ship_targetAsync);
        }

        private void CreateAndStartTimer(int interval, EventHandler handler)
        {
            var timer = new System.Windows.Forms.Timer { Interval = interval };
            timer.Tick += handler;
            timer.Start();
        }

        private void LoadServerSettings()
        {
            string dir = "./";
            try
            {
                if (!File.Exists(dir + "setting.dat"))
                {
                    File.WriteAllLines(dir + "setting.dat", new[] {
                        $"{Properties.Resources.DROP_SET_NAME} {drop_event_num.Text}",
                        $"{Properties.Resources.EXE_SET_NAME} {exp_event_num.Text}"
                    });
                }

                foreach (var line in File.ReadLines(dir + "setting.dat"))
                {
                    var data = line.Split(' ');
                    if (data[0] == Properties.Resources.EXE_SET_NAME)
                    {
                        exp_event_num.Text = data[1];
                        double.TryParse(data[1], out exe_event);
                    }
                    else if (data[0] == Properties.Resources.DROP_SET_NAME)
                    {
                        drop_event_num.Text = data[1];
                        double.TryParse(data[1], out drop_event);
                    }
                    write_log($"{data[0]} : {data[1]}");
                }
            }
            catch (Exception e)
            {
                write_log(e.ToString());
            }
        }

        private void LoadServerVersion()
        {
            string dir = "./";
            try
            {
                if (!File.Exists(dir + "version.txt"))
                {
                    File.WriteAllText(dir + "version.txt", version);
                }
                version = File.ReadAllText(dir + "version.txt").Trim();
                write_log("현재 버전 : " + version);
            }
            catch (Exception e)
            {
                write_log(e.ToString());
            }
        }

        private void LoadEventSettings()
        {
            try
            {
                string dir = "./";
                if (!File.Exists(dir + "setting.dat"))
                {
                    File.WriteAllLines(dir + "setting.dat", new[] {
                        $"{Properties.Resources.DROP_SET_NAME} {drop_event_num.Text}",
                        $"{Properties.Resources.EXE_SET_NAME} {exp_event_num.Text}"
                    });
                }

                foreach (var line in File.ReadLines(dir + "setting.dat"))
                {
                    var data = line.Split(' ');
                    if (data[0] == Properties.Resources.EXE_SET_NAME)
                    {
                        exp_event_num.Text = data[1];
                        double.TryParse(data[1], out exe_event);
                    }
                    else if (data[0] == Properties.Resources.DROP_SET_NAME)
                    {
                        drop_event_num.Text = data[1];
                        double.TryParse(data[1], out drop_event);
                    }
                    write_log($"{data[0]} : {data[1]}");
                }
            }
            catch (Exception e)
            {
                write_log(e.ToString());
            }
        }

        public async void LoadIPBanSettings()
        {
            // 나중에 여기서 db로부터 ip밴 리스트 받아오기
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

            foreach (var word in abusiveWords)
            {
                string pattern = Regex.Escape(word); // 단어 자체에 정규식 특수 문자가 포함될 수 있으므로 이스케이프 처리
                message = Regex.Replace(message, pattern, "욜", RegexOptions.IgnoreCase);
            }

            return message;
        }

        private async void timer_tick(object sender, EventArgs e)
        {
            try
            {
                DateTime nowTime = DateTime.Now;
                label3.Text = "현재 시간 : " + nowTime.ToString("HH:mm:ss");

                // 비동기 작업을 모두 비동기로 처리
                await HandleMonsterRespawnAsync();
                await HandleAggroAsync(nowTime);
                await HandleItemCleanupAsync(nowTime);
                await HandleWeekendEventsAsync(nowTime);
            }
            catch (Exception ex)
            {
                write_log(ex.ToString());
            }
        }

        private async Task HandleMonsterRespawnAsync()
        {
            List<string> list = sd.respawnMonster2();
            if (list.Count > 0)
            {
                foreach (var s in list)
                {
                    var data = s.Split(',');
                    await Map_Packet("respawn", s, int.Parse(data[0]));
                }
            }
        }

        private async Task HandleAggroAsync(DateTime nowTime)
        {
            foreach (var map_players in MapUser2)
            {
                var select_players = map_players.Value.Where(p => !p.player.isAggroFree()).ToArray();

                if (select_players.Length <= 1) continue;

                int map_id = map_players.Key;
                List<int> aggroList = sd.aggroTimeMonster(map_id);
                if (aggroList.Count <= 0) continue;

                foreach (var id in aggroList)
                {
                    string player_name = select_players[random.Next(select_players.Length)].userName;
                    string msg = $"{id},{player_name}";
                    await Map_Packet("aggro", msg, map_id);
                }
            }
        }

        private async Task HandleItemCleanupAsync(DateTime nowTime)
        {
            if (nowTime.Minute == 0 && nowTime.Second == 0)
            {
                await Packet("chat", "맵의 모든 아이템들이 삭제 됩니다.");
                sd.DelAllItem();
            }
        }

        private async Task HandleWeekendEventsAsync(DateTime nowTime)
        {
            if ((nowTime.Hour == 0 && nowTime.Minute == 0 && nowTime.Second == 0) || !weekendEventTriggered)
            {
                weekendEventTriggered = true;
                switch (nowTime.DayOfWeek)
                {
                    case DayOfWeek.Monday:
                        await Packet("chat", "주말 이벤트가 끝났습니다!~");
                        await exe_event_sendAsync(0);
                        await drop_event_sendAsync(0);
                        break;
                    case DayOfWeek.Saturday:
                        await Packet("chat", "즐거운 토요일! 주말 이벤트가 시작됩니다!");
                        await exe_event_sendAsync(2);
                        await drop_event_sendAsync(1.2);
                        break;
                    case DayOfWeek.Sunday:
                        await Packet("chat", "일요일은 흑바온!");
                        await exe_event_sendAsync(3);
                        await drop_event_sendAsync(1.3);
                        break;
                }
            }
        }

        async void timer_tick2(object sender, EventArgs e) // 리붓용 타이머 이벤트
        {
            try
            {
                count_down--;
                textBox2.Text = count_down.ToString();
                int min = count_down / 60;
                if (count_down > 60 && reboot_min != min)
                {
                    reboot_min = min;
                    await Packet("chat", min + "분 후 리붓합니다. 안전한 곳으로 이동하시길 바랍니다.");
                }
                else if(count_down < 60)
                {
                    await Packet("chat", count_down + "초 후 리붓합니다. 안전한 곳으로 이동하시길 바랍니다.");
                }

                if (count_down <= 0) // 전체 강퇴
                {
                    await Packet("over", "비바람이 휘몰아치고 있습니다. 잠시만 기다려 주세요.");
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

        async void random_server_msg(object sender, EventArgs e) // 랜덤 도움 메시지
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
                await Packet("chat2", "[도움]" + msg);
            }
            catch
            {
                write_log(e.ToString());
            }
        }

        async void change_ship_targetAsync(object sender, EventArgs e)
        {
            try
            {
                int target = now_ship_target();
                if (target >= 0 && check_ship_target != target)
                {
                    check_ship_target = target;
                    await Packet("chat", $"배의 목적지가 '{(target == 0 ? "일본" : "고균도")}'(으)로 변경되었습니다.");
                }
            }
            catch (Exception ex)
            {
                write_log($"Error in change_ship_target: {ex.Message}");
            }
        }

        public int now_ship_target()
        {
            int t = DateTime.Now.Minute;
            int target = -1;

            if (IsInRange(t, 1, 9) || IsInRange(t, 21, 29) || IsInRange(t, 41, 49))
            {
                SetShipTarget("일본", 0, out target);
            }
            else if (IsInRange(t, 11, 19) || IsInRange(t, 31, 39) || t >= 51)
            {
                SetShipTarget("고균도", 1, out target);
            }

            return target;
        }

        private void SetShipTarget(string targetName, int targetValue, out int target)
        {
            ship_target_name.Text = targetName;
            target = targetValue;
        }

        private bool IsInRange(int value, int min, int max)
        {
            return value >= min && value <= max;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Initialize
            MapUser2 = new ConcurrentDictionary<int, List<UserThread>>();
            UserByNameDict = new ConcurrentDictionary<string, UserThread>();

            // Listen New User Connection
            Thread echo_thread = new Thread(Thread_NetWorkListeningAsync)
            {
                IsBackground = true // 백그라운드 스레드로 설정
            };
            echo_thread.Start();
        }

        #region Mulit-Thread Tcp/Ip Network

        // Listen New User Connection
        public async void Thread_NetWorkListeningAsync()
        {
            TcpListener Listener = null;
            TcpClient client = null;

            try
            {
                Listener = new TcpListener(IPAddress.Any, Int32.Parse(Properties.Resources.PORT));
                Listener.Start();

                while (true)
                {
                    // 비동기적으로 클라이언트 연결 수락
                    client = await Listener.AcceptTcpClientAsync();

                    // 새로운 클라이언트 처리
                    UserThread userThread = new UserThread(this);
                    await Task.Run(() => userThread.StartClient(client)); // 비동기 처리
                }
            }
            catch (Exception e)
            {
                write_log(e.ToString());
            }
        }
        #endregion

        // 모든 유저에게 전송하는 패킷
        public async Task Packet(string tag, string body, string userCode = "")
        {
            var usersToRemove = new List<UserThread>();

            foreach (var user in UserByNameDict.Values)
            {
                // 유효하지 않은 유저는 삭제
                if (user.userCode.Equals("*null*"))
                {
                    usersToRemove.Add(user);
                    continue;
                }

                if (user.userCode.Equals(userCode)) continue;

                try
                {
                    await user.SendMessageWithTagAsync(tag, body);
                }
                catch (Exception)
                {
                    usersToRemove.Add(user);
                }
            }

            foreach (var user in usersToRemove)
            {
                await removethread(user);
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
        public async Task Map_Packet(string tag, string body, int map_id, string userCode = "")
        {
            if (!MapUser2.TryGetValue(map_id, out var users)) return;

            var usersToRemove = new List<UserThread>();

            foreach (var user in users)
            {
                if (user.userCode.Equals("*null*"))
                {
                    usersToRemove.Add(user);
                    continue;
                }

                if (user.userCode.Equals(userCode)) continue;

                try
                {
                    await user.SendMessageWithTagAsync(tag, body);
                }
                catch (Exception)
                {
                    usersToRemove.Add(user);
                }
            }

            foreach (var user in usersToRemove)
            {
                await removethread(user);
            }
        }

        public UserThread findMember(string name)
        {
            UserByNameDict.TryGetValue(name, out var userThread);
            return userThread;
        }

        // 유저의 맵 이름을 변경한다.
        public async Task editUserMapAsync(UserThread userThread, int new_id)
        {
            int old_id = userThread.lastMapId;

            var isNewMap = false;
            var newMapList = await Task.Run(() => MapUser2.GetOrAdd(new_id, id =>
            {
                isNewMap = true;
                return new List<UserThread>();
            }));

            if (isNewMap || newMapList.Count == 0)
            {
                await Task.Run(() => userThread.SendMessageWithTagAsync("map_player", "1"));
            }

            lock (newMapList)
            {
                newMapList.Add(userThread);
            }

            await removeMapUserAsync(old_id, userThread);
            await PlayerCountAsync();
        }

        public async Task removeMapUserAsync(int map_id, UserThread userThread)
        {
            if (!MapUser2.TryGetValue(map_id, out var users)) return;

            if (users.Remove(userThread) && users.Count > 0)
            {
                var firstUser = users[0];
                if (firstUser.client.Connected)
                {
                    await firstUser.SendMessageWithTagAsync("map_player", "1");
                }
            }
        }

        public int countMapUser(int map_id)
        {
            if (!MapUser2.TryGetValue(map_id, out var users)) return 0;
            lock (users)
            {
                return users.Count();
            }
        }

        // 유저 리스트에서 제거한다.
        public async Task removethread(UserThread userthread)
        {
            if (userthread == null) return;

            int map_id = userthread.lastMapId;
            await removeMapUserAsync (map_id, userthread);

            if (!string.IsNullOrEmpty(userthread.userName))
            {
                UserByNameDict.TryRemove(userthread.userName, out _);
                write_log($"{userthread.userName} 종료");
                await Packet("chat1", $"(알림): '{userthread.userName}'님께서 종료하셨습니다.");
            }

            await PlayerCountAsync();
            await Packet("9", userthread.userCode);
        }

        // 접속중인 아이디를 체크한다.
        public bool Checkid(string id)
        {
            return UserByNameDict.Values.Any(user => user.userId != null && user.userId.Equals(id));
        }

        private void FormClose(object sender, FormClosedEventArgs e)
        {
            write_log("서버 종료");
            write_log("------------------------------");
            Application.ExitThread();
            Environment.Exit(0);
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        public async Task PlayerCountAsync()
        {
            CheckForIllegalCrossThreadCalls = false;
            var userCount = await Task.Run(() => UserByNameDict.Count);

            // UI 업데이트는 UI 스레드에서 처리
            await UpdateUIAsync(userCount);
        }

        private async Task UpdateUIAsync(int userCount)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(async () => await UpdateUIAsync(userCount))); // UI 스레드에서 실행
                return;
            }

            label7.Text = "접속자 수 : " + userCount;

            listBox1.BeginUpdate();
            var existingUsers = new List<string>();

            // lock 대신 SemaphoreSlim을 사용하여 비동기 호환성을 높임
            await _userLock.WaitAsync();
            try
            {
                foreach (var user in UserByNameDict.Values)
                {
                    string userEntry = $"{user.userName}: {user.mapName}";
                    existingUsers.Add(user.userName);

                    bool found = false;
                    for (int i = 0; i < listBox1.Items.Count; i++)
                    {
                        string listItem = listBox1.Items[i].ToString();
                        if (listItem.StartsWith(user.userName + ":"))
                        {
                            listBox1.Items[i] = userEntry;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        listBox1.Items.Add(userEntry);
                    }
                }

                for (int i = listBox1.Items.Count - 1; i >= 0; i--)
                {
                    string listItem = listBox1.Items[i].ToString();
                    string listUserName = listItem.Split(':')[0];

                    if (!existingUsers.Contains(listUserName))
                    {
                        listBox1.Items.RemoveAt(i);
                    }
                }
            }
            finally
            {
                _userLock.Release();
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

        private async Task handle_prisonAsync()
        {
            if (listBox1.SelectedIndex < 0) return;

            string name = listBox1.SelectedItem.ToString().Split(':')[0];
            await Packet("prison", name);
            write_log(name + " 감옥");
            textBox1.Text = "";
            await Packet("chat", $"{name}님께서 감옥으로 갔습니다.");
        }

        private async Task handle_emancipationAsync()
        {
            if (listBox1.SelectedIndex < 0) return;

            string name = listBox1.SelectedItem.ToString().Split(':')[0];
            await Packet("emancipation", name);
            write_log(name + " 석방");
            textBox1.Text = "";
            await Packet("chat", $"{name}님께서 감옥에서 석방 되셨습니다.");
        }
        private async Task handle_kickAsync()
        {
            if (listBox1.SelectedIndex < 0) return;

            string name = listBox1.SelectedItem.ToString().Split(':')[0];
            string msg = textBox1.Text.Equals("") ? "강퇴 당하셨습니다." : textBox1.Text;
            await Packet("ki", name + "," + msg);
            await Packet("chat", name + "님이 강퇴 당하셨습니다.");
            write_log(name + " 강퇴");
            textBox1.Text = "";
        }

        // 공지 보내기
        private async void button1_Click(object sender, EventArgs e)
        {
            if (radioButton_1.Checked) // 공지
            {
                if (!string.IsNullOrWhiteSpace(textBox1.Text))
                {
                    await Packet("chat", "(공지) : " + textBox1.Text); // await 적용
                    textBox1.Text = "";
                }
            }
            else if (radioButton_2.Checked) // 감옥
            {
                await handle_prisonAsync(); // await 추가
            }
            else if (radioButton_3.Checked) // 석방
            {
                await handle_emancipationAsync(); // await 추가
            }
            else if (radioButton_4.Checked) // 유저 강퇴
            {
                await handle_kickAsync(); // 
            }
            else if (radioButton_5.Checked) // 모두 강퇴
            {
                await Packet("ki", "모두," + textBox1.Text); // await 추가
                write_log("모두 강퇴 : " + textBox1.Text);
                textBox1.Text = "";
            }
            autoListBoxScroll();
        }

        public void autoListBoxScroll()
        {
            if (listBox2.InvokeRequired)
            {
                listBox2.Invoke(new Action(autoListBoxScroll));
            }
            else
            {
                // 자동 스크롤
                int visibleItems = listBox2.ClientSize.Height / listBox2.ItemHeight;

                if (Math.Abs(listBox2.TopIndex - listBox2.Items.Count + visibleItems) <= 3)
                    listBox2.TopIndex = Math.Max(listBox2.Items.Count - 1, 0);
            }
        }

        private void message_keyDown(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                this.button1_Click(sender, e);
            }
        }

        private async Task HandleEventSendAsync(object sender, KeyPressEventArgs e, string eventType, TextBox eventNumTextBox, string logMessage, string settingResourceName, double eventValue)
        {
            if (e.KeyChar == '\r')
            {
                if (double.TryParse(eventNumTextBox.Text, out double n))
                {
                    await Packet(eventType, n.ToString());
                    write_log($"{logMessage} {n}배 이벤트 시작");
                    eventValue = n;

                    autoListBoxScroll();
                    UpdateSettingFile(settingResourceName, n);
                }
            }
        }

        private async Task HandleEventSendAsync(string eventType, TextBox eventNumTextBox, string logMessage, string settingResourceName, double eventValue)
        {
            if (double.TryParse(eventNumTextBox.Text, out double n))
            {
                await Packet(eventType, n.ToString());
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

        // 동기 이벤트 핸들러
        private void exp_event_num_KeyPress(object sender, KeyPressEventArgs e)
        {
            // 내부에서 비동기 메서드를 호출
            _ = exe_event_sendAsync(sender, e);
        }

        // 동기 이벤트 핸들러
        private void drop_event_num_KeyPress(object sender, KeyPressEventArgs e)
        {
            // 내부에서 비동기 메서드를 호출
            _ = drop_event_sendAsync(sender, e);
        }

        private async Task exe_event_sendAsync(object sender, KeyPressEventArgs e)
        {
            await HandleEventSendAsync(sender, e, "exp_event", exp_event_num, "경험치", Properties.Resources.EXE_SET_NAME, exe_event);
        }
        public async Task exe_event_sendAsync(double num)
        {
            exp_event_num.Text = num.ToString();
            exe_event = num;
            await HandleEventSendAsync("exp_event", exp_event_num, "경험치", Properties.Resources.EXE_SET_NAME, exe_event);
        }

        private async Task drop_event_sendAsync(object sender, KeyPressEventArgs e)
        {
            await HandleEventSendAsync(sender, e, "drop_event", drop_event_num, "드랍율", Properties.Resources.DROP_SET_NAME, drop_event);
        }
        public async Task drop_event_sendAsync(double num)
        {
            drop_event_num.Text = num.ToString();
            drop_event = num;
            await HandleEventSendAsync("drop_event", drop_event_num, "드랍율", Properties.Resources.DROP_SET_NAME, drop_event);
        }

        private void ListBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && listBox1.SelectedIndex >= 0)
            {
                var contextMenu = new ContextMenu(new[]
                {
            new MenuItem("감옥", async (s, ev) => await MenuClickAsync(s, ev)),
            new MenuItem("석방", async (s, ev) => await MenuClickAsync(s, ev)),
            new MenuItem("강퇴", async (s, ev) => await MenuClickAsync(s, ev)),
            new MenuItem("Ip밴", async (s, ev) => await MenuClickAsync(s, ev)),
            new MenuItem("Ip밴 해제", async (s, ev) => await MenuClickAsync(s, ev)),
        });
                ContextMenu = contextMenu;
            }
        }

        private async Task MenuClickAsync(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex >= 0)
            {
                var selectedItem = listBox1.SelectedItem.ToString().Split(':')[0];
                var action = (sender as MenuItem)?.Text;

                switch (action)
                {
                    case "감옥":
                        await handle_prisonAsync();
                        break;
                    case "석방":
                        await handle_emancipationAsync();
                        break;
                    case "강퇴":
                        await handle_kickAsync();
                        break;
                    case "Ip밴":
                        await handleBanIPAsync();
                        break;
                    case "Ip밴 해제":
                        await handleUnBanIPAsync();
                        break;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            double n = -1;
            if (double.TryParse(textBox2.Text, out n))
            {
                if (timer != null && timer.Enabled) return;

                timer = new System.Windows.Forms.Timer();
                timer.Interval = 1000; // 1초마다 타이머 시작함

                count_down = (int)(n * 60); // n분만큼 저장됨
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
            if (string.IsNullOrWhiteSpace(s)) return;

            var logDirectory = "./LogServer/";
            Directory.CreateDirectory(logDirectory);

            var timestamp = DateTime.Now;
            var logFileName = $"{logDirectory}({timestamp.ToShortDateString()})Log.txt";
            var logEntry = $"[{timestamp}] {s}";

            lock (_userLock)
            {
                if (listBox2.InvokeRequired)
                {
                    listBox2.Invoke(new Action(() => listBox2.Items.Add(logEntry)));
                }
                else
                {
                    listBox2.Items.Add(logEntry);
                }
            }

            try
            {
                File.AppendAllText(logFileName, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                string errorMessage = $"[Error] 로그 쓰기 실패: {ex.Message}";

                if (listBox2.InvokeRequired)
                {
                    listBox2.Invoke(new Action(() => listBox2.Items.Add(errorMessage)));
                }
                else
                {
                    listBox2.Items.Add(errorMessage);
                }
            }
        }

        // 맵의 유저들에게 스위치 공유
        public async Task switch_sendAsync(string sw_num, string state, int map_id = 0)
        {
            string msg = sw_num + "," + state;
            if (map_id > 0)
                await Map_Packet("switches", msg, map_id);
            else
                await Packet("switches", msg);
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
        public void write_log_user(string name, string data, string dirName = "default")
        {
            try
            {
                string date = DateTime.Now.ToString("yyyy-MM-dd");
                string time = DateTime.Now.ToString("HH:mm:ss");

                // 유저별 로그 디렉토리 경로
                string dir = Path.Combine("./LogUser", name);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                
                // 로그 파일 경로 (날짜별로 구분하여 저장)
                string dir2 = Path.Combine(dir, date);
                if (!Directory.Exists(dir2)) Directory.CreateDirectory(dir2);

                string defaultDir = Path.Combine(dir2, dirName);
                if (!Directory.Exists(defaultDir)) Directory.CreateDirectory(defaultDir);

                string logFilePath = Path.Combine(defaultDir, $"{date}_{name}_Log.txt");

                // 파일 크기 제한: 5MB 이상이면 새로운 파일로 분리
                const long maxSize = 5 * 1024 * 100; // 500kb
                if (File.Exists(logFilePath) && new FileInfo(logFilePath).Length >= maxSize)
                {
                    string newLogFilePath = Path.Combine(defaultDir, $"{date}_{time}_{name}_Log.txt");
                    logFilePath = newLogFilePath;
                }

                // 로그 파일에 기록
                using (StreamWriter logfile = new StreamWriter(logFilePath, true))
                {
                    if (!string.IsNullOrEmpty(data))
                    {
                        logfile.WriteLine($"[{time}] {data}");
                    }
                }

                // 필요 시 자동 스크롤 (UI가 있으면)
                autoListBoxScroll();
            }
            catch (Exception ex)
            {
                // 예외 처리 (필요 시 로그 출력)
                Console.WriteLine("Error writing log: " + ex.Message);
            }
        }

        public async Task handleBanIPAsync()
        {
            if (listBox1.SelectedIndex < 0) return;

            string name = listBox1.SelectedItem.ToString().Split(':')[0];
            UserThread user = UserByNameDict[name];
            BanIP(user.ipAddress);
            await Packet("chat", $"{name}님께서 차단 되었습니다.");
        }

        public async Task handleUnBanIPAsync()
        {
            if (listBox1.SelectedIndex < 0) return;

            string name = listBox1.SelectedItem.ToString().Split(':')[0];
            UserThread user = UserByNameDict[name];
            UnbanIP(user.ipAddress);
            await Packet("chat", $"{name}님께서 차단 해제되었습니다.");
        }

        // IP 차단 메서드
        public void BanIP(string ipAddress)
        {
            if (!bannedIPs.Contains(ipAddress))
            {
                bannedIPs.Add(ipAddress);
                write_log($"IP {ipAddress}가 차단되었습니다.");
            }
        }

        // IP 차단 해제 메서드 (선택 사항)
        public void UnbanIP(string ipAddress)
        {
            if (bannedIPs.Contains(ipAddress))
            {
                bannedIPs.Remove(ipAddress);
                write_log($"IP {ipAddress}가 차단 해제되었습니다.");
            }
        }

        // 유저 접속 시 IP 확인
        public bool IsIPBanned(string ipAddress)
        {
            return bannedIPs.Contains(ipAddress);
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        // 유저 접속 처리 메서드 (예시)
        //public void HandleUserConnection(string ipAddress, UserThread userThread)
        //{
        //    if (IsIPBanned(ipAddress))
        //    {
        //        userThread.SendMessage("접속이 차단되었습니다. 관리자에게 문의하세요.");
        //        userThread.Disconnect(); // 차단된 IP는 연결 끊기
        //        write_log($"차단된 IP {ipAddress}의 연결이 거부되었습니다.");
        //    }
        //    else
        //    {
        //        // 정상 접속 처리 로직
        //        write_log($"IP {ipAddress}가 접속했습니다.");
        //    }
        //}
    }
}
