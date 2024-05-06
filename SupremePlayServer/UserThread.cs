using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SupremePlayServer
{
    public class UserThread
    {
        // Network Stream
        public NetworkStream NS;
        public StreamReader SR;
        public StreamWriter SW;
        public TcpClient client;

        public mainForm mainForm;
        public string userCode = "*null*";
        public string userId;
        public string userName;
        public int lastMapId = 0;
        public string mapName;
        public bool isVersionValid = false;

        private Systemdata systemData;
        private System_DB systemDB;
        public Thread thread;

        private System.Timers.Timer versionCheckTimer;
        public PartyManager partyManger;
        public TradeManager tradeManager;

        public void StartClient(TcpClient clientSocket)
        {
            InitializeClient(clientSocket);
            StartListenerThread();
            StartVersionCheckTimer();
        }

        private void InitializeClient(TcpClient clientSocket)
        {
            systemData = mainForm.sd;
            systemDB = mainForm.systemDB;
            userCode = new Random().Next(0, 9999999).ToString();
            partyManger = new PartyManager(this);
            tradeManager = new TradeManager(this);

            client = clientSocket;
            NS = client.GetStream();
            SR = new StreamReader(NS, Encoding.UTF8);
            SW = new StreamWriter(NS, Encoding.UTF8);
        }

        private void StartListenerThread()
        {
            thread = new Thread(NetListener) { IsBackground = true };
            thread.Start();
        }

        private void StartVersionCheckTimer()
        {
            versionCheckTimer = new System.Timers.Timer();
            versionCheckTimer.Interval = 5000;
            versionCheckTimer.Elapsed += new System.Timers.ElapsedEventHandler(VersionCheckTimerTick);
            versionCheckTimer.AutoReset = false;
        }

        private void VersionCheckTimerTick(object sender, EventArgs e)
        {
            mainForm.write_log("카운트다운 끝");
            if (!isVersionValid)
            {
                mainForm.write_log("version_false");
                SendMessageWithTag("over", "버전이 다릅니다.");
                CloseClient();
            }
            else
            {
                mainForm.write_log("version_true");
            }

            versionCheckTimer.Stop();
        }


        // Thread - Net Listener
        private void NetListener()
        {
            try
            {
                while (client.Connected)
                {
                    string receivedMessage = SR.ReadLine();
                    if (string.IsNullOrEmpty(receivedMessage)) continue;

                    HandleMessage(receivedMessage);
                }
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
            }
            finally
            {
                CloseClient();
            }
        }

        private void HandleMessage(string message)
        {
            string[] flag = { ">" };
            String[] d1 = message.Split(flag, StringSplitOptions.RemoveEmptyEntries);
            if (d1.Length <= 1) return;

            string tag = d1[0].Substring(1);
            string body = splitTag(tag, message);

            // 로그에 메시지 저장
            if (!string.IsNullOrEmpty(userName))
            {
                if (!systemData.ignoreMessageDict.ContainsKey(tag)) mainForm.write_log_user(userName, message);
            }

            switch(tag)
            {
                case "0":
                    SendMessageWithTag("0", userCode + " 'e' n=Suprememay Server");
                    break;
                case "login":
                    HandleLogin(tag, body);
                    break;
                case "regist":
                    systemDB.Registeration(this, tag, body);
                    break;
                case "versione":
                    HandleVersionCheck(tag, body);
                    break;
                case "2":
                    SendMessageWithTag(tag, userId);
                    break;
                case "check":
                    SendMessageWithTag(tag, "standard");
                    break;
                case "timer_v":
                    if (body == "ok") isVersionValid = true;
                    break;
                case "userdata":
                    systemDB.SaveData2(body, userId);
                    break;
                case "exp_event":
                    HandleEvent(tag, body);
                    break;
                case "drop_event":
                    HandleEvent(tag, body);
                    break;
                case "chat":
                    mainForm.Packet(tag, body);
                    break;
                case "5":
                    mainForm.Packet("5", userCode + ","+ body, userCode);
                    break;
                case "m5":
                    mainForm.Map_Packet("5", userCode + "," + body, lastMapId, userCode);
                    break;
                case "dtloadreq":
                    systemDB.SendData(this, userId);
                    break;
                case "monster_save":
                    systemData.SaveMonster(body);
                    mainForm.Map_Packet(tag, body, lastMapId, userCode);
                    break;
                case "enemy_dead":
                    systemData.DeleteMonster(body);
                    mainForm.Map_Packet(tag, body, lastMapId, userCode);
                    break;
                case "req_monster":
                    HandleReqMonster(tag, body);
                    break;
                case "attack_effect":
                    {
                        string[] temp = { "," };
                        String[] data = body.Split(temp, StringSplitOptions.RemoveEmptyEntries);
                        string target = data[0];

                        if (!mainForm.UserByNameDict.ContainsKey(target)) return;
                        mainForm.UserByNameDict[target].SendMessageWithTag(tag, userCode);
                        break;
                    }
                case "skill_effect":
                    {
                        string[] temp = { "," };
                        String[] data = body.Split(temp, StringSplitOptions.RemoveEmptyEntries);
                        string target = data[0];
                        string skill_id = data[1];

                        if (!mainForm.UserByNameDict.ContainsKey(target)) return;
                        mainForm.UserByNameDict[target].SendMessageWithTag(tag, userCode + "," + skill_id);
                        break;
                    }
                case "e_skill_effect":
                    {
                        string[] temp = { "," };
                        String[] data2 = body.Split(temp, StringSplitOptions.RemoveEmptyEntries);

                        string target = data2[0];
                        string enemy_id = data2[1];
                        string skill_id = data2[2];
                        if (!mainForm.UserByNameDict.ContainsKey(target)) return;
                        mainForm.UserByNameDict[target].SendMessageWithTag(tag, enemy_id + "," + skill_id);
                    }
                    break;
                case "post":
                    {
                        Dictionary<string, string> dict = ParseKeyValueData(body);
                        string target = dict["target_name"];
                        if (!mainForm.UserByNameDict.ContainsKey(target)) return;
                        mainForm.UserByNameDict[target].SendMessageWithTag(tag, body);
                    }
                    break;
                case "Drop":
                    systemData.SaveItem(body);
                    mainForm.Map_Packet(tag, body, lastMapId);
                    break;
                case "Drop_Get":
                    systemData.DelItem2(body);
                    mainForm.Map_Packet(tag, body, lastMapId);
                    break;
                case "req_item":
                    {
                        if (!systemData.item_data2.ContainsKey(lastMapId)) return;
                        List<Item> dat = systemData.item_data2[lastMapId];
                        string msg2 = "";
                        foreach (var d in dat)
                        {
                            // 객체의 타입을 가져옴
                            Type type = d.GetType();

                            foreach (var field in type.GetFields())
                            {
                                object value = field.GetValue(d); // 필드의 값 가져오기
                                msg2 += $"{field.Name}:{value}|";
                            }
                            SendMessageWithTag("Drop", msg2);
                        }
                    }
                    break;
                case "item_summon":
                    {
                        string[] temp = { "," };
                        String[] data2 = body.Split(temp, StringSplitOptions.RemoveEmptyEntries);

                        string target = data2[0];
                        string x = data2[1];
                        string y = data2[2];

                        if (!mainForm.UserByNameDict.ContainsKey(target)) return;
                        mainForm.UserByNameDict[target].SendMessageWithTag(tag, x + "," + y);
                    }
                    break;
                case "map_name":
                    {
                        systemDB.SaveMap(message);
                        string[] temp = { "," };
                        String[] data2 = body.Split(temp, StringSplitOptions.RemoveEmptyEntries);

                        int map_id = int.Parse(data2[0]);
                        int flag_id = 0;
                        if (!mainForm.MapUser2.ContainsKey(map_id)) // 해당 맵에 아무도 없었다면?
                        {
                            mainForm.MapUser2.Add(map_id, new List<UserThread>());
                        }

                        if (mainForm.MapUser2[map_id].Count == 0) flag_id = 1;
                        else flag_id = 0;
                        SendMessageWithTag("map_player", flag_id.ToString());

                        mainForm.MapUser2[map_id].Add(this);
                        mainForm.removeMapUser(lastMapId, this); // 이전에 있었던 리스트에서 제거함

                        lastMapId = map_id;
                        mapName = systemData.SendMap(lastMapId);
                        mainForm.PlayerCount();
                    }
                    break;
                case "9":
                    CloseClient();
                    break;

                // 교환 관련
                case "trade_invite":
                    tradeManager.inviteTrade(body);
                    break;
                case "trade_addItem":
                    tradeManager.addItem(ParseKeyValueData(body));
                    break;
                case "trade_removeItem":
                    tradeManager.removeItem(int.Parse(body));
                    break;
                case "trade_ready":
                    tradeManager.readyTrade();
                    break;
                case "trade_cancel":
                    tradeManager.cancelTrade();
                    break;
                case "trade_accept":
                    tradeManager.acceptTrade();
                    break;
                case "trade_refuse":
                    tradeManager.refuseTrade();
                    break;




                // 파티 관련
                case "party_create":
                    partyManger.createParty();
                    break;

                case "party_end":
                    partyManger.endParty();
                    break;

                case "party_invite":
                    {
                        Dictionary<string, string> dict = ParseKeyValueData(body);
                        string target = dict["target"];
                        partyManger.inviteParty(target);
                    }
                    break;

                case "party_accept":
                    {
                        partyManger.acceptParty();
                    }
                    break;

                case "party_refuse":
                    {
                        partyManger.refuseParty();
                    }
                    break;

                case "party_switch":
                    {
                        // 스위치 id, 스위치 상태, 맵 id
                        string[] temp = { "," };
                        String[] data2 = body.Split(temp, StringSplitOptions.RemoveEmptyEntries);
                        mainForm.switch_send(data2[0], data2[1], int.Parse(data2[2]));
                    }
                    break;
                case "party_quest_check":
                    {
                        int map_id;
                        if (!int.TryParse(body, out map_id)) return;
                        
                        int[] check = systemData.checkPartyQuest(map_id);
                        if (check[0] == 0) return;

                        SendMessageWithTag(tag, check[0] + "," + check[1]);
                    }
                    break;
                case "party_move":
                    {
                        SendMessageToPartyMembers(
                            (member) => member.lastMapId == this.lastMapId,
                            (member) => member.SendMessageWithTag(tag, body)
                            ); 
                    }
                    break;
                case "party_message":
                    {
                        var dict = ParseKeyValueData(body);
                        string className = dict["class"];
                        string text = dict["text"];
                        string msg = $"(파티) {userName}({className}) : {text}";

                        SendMessageToPartyMembers(
                            (member) => !member.Equals(userName),
                            (member) => member.SendMessageWithTag(tag, msg)
                            );
                    }
                    break;
                case "party_heal":
                    {
                        var dict = ParseKeyValueData(body);
                        string id = dict["id"];
                        string value = dict["value"];
                        string msg = $"{userName} {id} {value}";

                        SendMessageToPartyMembers(
                            (member) => !member.Equals(userName),
                            (member) => member.SendMessageWithTag(tag, msg)
                            );
                    }
                    break;

                case "party_gain":
                    SendMessageToPartyMembers(
                           (member) => !member.Equals(userName) && (member.lastMapId == this.lastMapId),
                           (member) => member.SendMessageWithTag(tag, body)
                           );
                    break;


                case "ship_time_check":
                    SendMessageWithTag(tag, mainForm.now_ship_target().ToString());
                    break;
                case "monster_cooltime_reset":
                    {
                        // 스위치 id, 스위치 상태, 맵 id
                        string[] temp = { "," };
                        String[] data2 = body.Split(temp, StringSplitOptions.RemoveEmptyEntries);
                        if (data2.Length >= 2)
                            mainForm.monster_cooltime_reset(int.Parse(data2[0]), int.Parse(data2[1]));
                        else
                            mainForm.monster_cooltime_reset(int.Parse(data2[0]));
                    }
                    break;

               
                case "whispers":
                    {
                        string[] temp = { "," };
                        String[] data2 = body.Split(temp, StringSplitOptions.RemoveEmptyEntries);

                        string target = data2[0];
                        string ms = data2[1];

                        if (!mainForm.UserByNameDict.ContainsKey(target))
                        {
                            SendMessageWithTag(tag, "귓속말 할 상대가 없습니다.");
                            return;
                        }

                        string msg = $"(귓속말) {userName} : {ms}";

                        mainForm.UserByNameDict[target].SendMessageWithTag(tag, msg);
                        ms = $"{userName}->{target}:{ms}";
                        mainForm.write_log(ms);
                    }
                    break;
               
                default:
                    {
                        string check = "<" + tag + ">";
                        if (systemData.packetMessageDict.ContainsKey(check)) mainForm.Packet(tag, body, userCode);
                        else if (systemData.mapPacketMessageDict.ContainsKey(check)) mainForm.Map_Packet(tag, body, lastMapId, userCode);
                    }
                    break;
            }
        }

        private void HandleLogin(string tag, string body)
        {
            string loginResult = systemDB.Login(body);
            string[] loginData = loginResult.Split(',');
            int resultCode = int.Parse(loginData[2]);

            bool isDuplicate = mainForm.Checkid(loginData[1]);
            
            if (isDuplicate)
            {
                SendMessageWithTag(tag, "al,1"); // Already logged in
                return;
            }
            
            if (resultCode == 0) SendMessageWithTag(tag, "wu,1"); // Wrong username
            else if (resultCode == 1) SendMessageWithTag(tag, "wp,1"); // Wrong password
            else if (resultCode == 2) CompleteLogin(loginData);
        }

        private void CompleteLogin(string[] loginData)
        {
            if (mainForm.UserByNameDict.Count > mainForm.max_user_name)
            {
                SendMessageWithTag("server_msg", "서버 유저 수 제한입니다. 다음에 시도해주세요.");
                return;
            }

            userName = loginData[0];
            userId = loginData[1];
            mainForm.UserByNameDict[userName] = this;
            SendMessageWithTag("login", "allow," + userName);
            SendMessageWithTag("server_msg", "흑부엉의 바람의나라에 오신 것을 환영합니다.");
        }

        private void HandleVersionCheck(string tag, string body)
        {
            if (body != mainForm.version)
            {
                SendMessageWithTag("over", "버전이 다릅니다.");
            }
            else
            {
                mainForm.write_log("카운트다운 시작");
                versionCheckTimer.Start();
                SendMessageWithTag(tag, mainForm.version); // 5초 내에 응답하지 않으면 퇴출
                SendMessageWithTag("timer_v", "");
            }
        }

        private void HandleEvent(string tag, string body)
        {
            switch(tag)
            {
                case "exp_event":
                    if (mainForm.exe_event > 0) SendMessageWithTag(tag, mainForm.exe_event.ToString());
                    break;
                case "drop_event":
                    if (mainForm.drop_event > 0) SendMessageWithTag(tag, mainForm.drop_event.ToString());
                    break;
            }
        }

        private void HandleReqMonster(string tag, string body)
        {
            if (!systemData.monster_data.ContainsKey(lastMapId))
            {
                SendMessageWithTag(tag, lastMapId.ToString());
                return;
            }

            var da = systemData.monster_data[lastMapId].Values;
            string msg = "";
            foreach (var d in da)
            {
                // 객체의 타입을 가져옴
                Type type = d.GetType();

                foreach (var field in type.GetFields())
                {
                    object value = field.GetValue(d); // 필드의 값 가져오기
                    msg += $"{field.Name}:{value}|";
                }
                SendMessageWithTag(tag, msg);
            }
        }

        public void SendMessageWithTag(string tag, string body = "")
        {
            string startTag = "<" + tag + ">";
            string endTag = "</" + tag + ">";
            string message = startTag + body + endTag;
            this.SW.WriteLine(message);
            this.SW.Flush();
        }

        public void SendConsoleMessage(string body)
        {
            SendMessageWithTag("console_msg", body);
        }

        private void SendMessageToPartyMembers(Func<UserThread, bool> condition, Action<UserThread> action)
        {
            foreach (var member in partyManger.partyMembers)
            {
                if (member == null) continue;

                if (condition(member))
                {
                    action(member);
                }
            }
        }


        public void CloseClient()
        {
            try
            {
                mainForm.removethread(this);
                userCode = "*null*";

                SW?.Dispose();
                SR?.Dispose();
                NS?.Dispose();
                client?.Close();  // IDisposable이 아닌 경우에도 처리
                thread?.Join();
            }
            catch (Exception ex)
            {
                mainForm.write_log(ex.ToString());
            }
        }

        public string splitTag(string tag, string data)
        {
            string startTag = "<" + tag + ">";
            string endTag = "</" + tag + ">";

            int startIndex = data.IndexOf(startTag) + startTag.Length;
            int endIndex = data.IndexOf(endTag);

            return data.Substring(startIndex, endIndex - startIndex);
        }

        public Dictionary<string, string> ParseKeyValueData(string data)
        {
            var result = new Dictionary<string, string>();
            var pairs = data.Split('|');

            foreach (var pair in pairs)
            {
                var keyValue = pair.Split(':');
                if (keyValue.Length == 2)
                {
                    result[keyValue[0]] = keyValue[1];
                }
            }

            return result;
        }
    }
}
