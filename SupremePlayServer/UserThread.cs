using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        public Player player;

        public IPEndPoint ip_point;
        public string ipAddress;

        public UserThread(mainForm mainForm)
        {
            this.mainForm = mainForm;
        }

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
            player = new Player();

            client = clientSocket;
            ip_point = (IPEndPoint)client.Client.RemoteEndPoint;
            ipAddress = ip_point.Address.ToString();

            NS = client.GetStream();
            SR = new StreamReader(NS, Encoding.UTF8, false, 1024 * 8); // 8KB 버퍼
            SW = new StreamWriter(NS, Encoding.UTF8, 1024 * 8) { AutoFlush = true }; // 8KB 버퍼, 자동 Flush
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
            versionCheckTimer.Elapsed += async (sender, e) => await VersionCheckTimerTickAsync();
            versionCheckTimer.AutoReset = false;
        }

        private async Task VersionCheckTimerTickAsync()
        {
            mainForm.write_log("카운트다운 끝");
            if (!isVersionValid)
            {
                mainForm.write_log("version_false");
                await SendMessageWithTagAsync("over", "버전이 다릅니다.");
                await CloseClientAsync(true);
            }
            else
            {
                mainForm.write_log("version_true");
            }

            versionCheckTimer.Stop();
        }


        // Thread - Net Listener
        private async void NetListener()
        {
            try
            {
                while (client.Connected)
                {
                    string receivedMessage = await SR.ReadLineAsync();
                    if (string.IsNullOrEmpty(receivedMessage)) continue;

                    await HandleMessage(receivedMessage);
                }
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
            }
            finally
            {
                await CloseClientAsync();
            }
        }


        private async Task HandleMessage(string message)
        {
            if (message.Length < 3 || message[0] != '<') return;

            int endOfTag = message.IndexOf('>');
            if (endOfTag < 0) return;

            string tag = message.Substring(1, endOfTag - 1);
            string body = splitTag(tag, message);

            // 로그에 메시지 저장
            if (!string.IsNullOrEmpty(userName))
            {
                if (systemData.logMessageDict.ContainsKey($"<{tag}>"))
                {
                    string log_file_name = "default";
                    if (!string.IsNullOrEmpty(systemData.logMessageDict[$"<{tag}>"]))
                        log_file_name = systemData.logMessageDict[$"<{tag}>"];
                    await Task.Run(() => mainForm.write_log_user(userName, message, log_file_name));
                }
            }

            switch (tag)
            {
                case "important_log":
                    HandleImportantLog(body);
                    break;

                case "0":
                    await SendMessageWithTagAsync("0", userCode + " 'e' n=Suprememay Server");
                    break;
                case "login":
                    await HandleLoginAsync(tag, body);
                    break;
                case "regist":
                    await systemDB.RegisterationAsync(this, tag, body);
                    break;
                case "versione":
                    await HandleVersionCheck(tag, body);
                    break;
                case "2":
                    await SendMessageWithTagAsync(tag, userId);
                    break;
                case "check":
                    await SendMessageWithTagAsync(tag, "standard");
                    break;
                case "timer_v":
                    if (body == "ok") isVersionValid = true;
                    break;
                case "userdata":
                    await systemDB.SaveData2Async(body, this);
                    break;

                case "exp_event":
                case "drop_event":
                case "exp_event_change":
                case "drop_event_change":
                    await HandleEventAsync(tag, body);
                    break;

                case "5":
                    await mainForm.Packet("5", userCode + ","+ body, userCode);
                    break;
                case "m5":
                    await mainForm.Map_Packet("5", userCode + "," + body, lastMapId, userCode);
                    break;

                case "aggro":
                    {
                        string[] d = body.Split(',');
                        int id = int.Parse(d[0]);
                        string name = d[1];

                        if (systemData.monster_data.ContainsKey(lastMapId) && systemData.monster_data[lastMapId].ContainsKey(id))
                        {
                            var monster = systemData.monster_data[lastMapId][id];
                            monster.aggroTime = monster.aggroResetTime;
                            await mainForm.Map_Packet("aggro", $"{id},{name}", lastMapId, userCode);
                        }
                        break;
                    }
                    
                case "give_admin":
                    {
                        UserThread target = mainForm.findMember(body);
                        if (target == null) return;

                        await target.SendMessageWithTagAsync(tag, body);
                        break;
                    }
                case "remove_admin":
                    {
                        UserThread target = mainForm.findMember(body);
                        if (target == null) return;

                        await target.SendMessageWithTagAsync(tag, body);
                        break;
                    }
                case "stealth":
                    {
                        if(body.Equals("1")) player.stealth = true;
                        else player.stealth = false;
                        break;
                    }

                case "check_in_map_player":
                    {
                        int map_id = int.Parse(body);
                        bool check = (mainForm.countMapUser(map_id) > 0);
                        await SendMessageWithTagAsync(tag, check ? "1" : "0");
                        break;
                    }
                    


                case "dtloadreq":
                    await systemDB.SendDataAsync(this, userId);
                    break;
                case "monster_save":
                    systemData.SaveMonster(body);
                    await mainForm.Map_Packet(tag, body, lastMapId, userCode);
                    break;
                case "enemy_dead":
                    systemData.DeleteMonster(body, lastMapId);
                    await mainForm.Map_Packet(tag, body, lastMapId, userCode);
                    break;
                
                case "npc_create":
                    systemData.SaveNpc(body);
                    await mainForm.Map_Packet(tag, body, lastMapId, userCode);
                    break;
                case "npc_delete":
                    systemData.DeleteNpc(body);
                    await mainForm.Map_Packet(tag, body, lastMapId, userCode);
                    break;
                

                case "attack_effect":
                    {
                        string[] temp = { "," };
                        String[] data = body.Split(temp, StringSplitOptions.RemoveEmptyEntries);
                        string target = data[0];

                        if (!mainForm.UserByNameDict.ContainsKey(target)) return;
                        await mainForm.UserByNameDict[target].SendMessageWithTagAsync(tag, userCode);
                        break;
                    }
                case "skill_effect":
                    {
                        string[] temp = { "," };
                        String[] data = body.Split(temp, StringSplitOptions.RemoveEmptyEntries);
                        string target = data[0];
                        string skill_id = data[1];

                        if (!mainForm.UserByNameDict.ContainsKey(target)) return;
                        await mainForm.UserByNameDict[target].SendMessageWithTagAsync(tag, userCode + "," + skill_id);
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
                        await mainForm.UserByNameDict[target].SendMessageWithTagAsync(tag, enemy_id + "," + skill_id);
                    }
                    break;
                case "post":
                    {
                        Dictionary<string, string> dict = ParseKeyValueData(body);
                        string target = dict["target_name"];
                        if (!mainForm.UserByNameDict.ContainsKey(target)) return;
                        await mainForm.UserByNameDict[target].SendMessageWithTagAsync(tag, body);
                    }
                    break;
                case "Drop":
                    systemData.SaveItem(body);
                    await mainForm.Map_Packet(tag, body, lastMapId);
                    break;
                case "Drop_Get":
                    systemData.DelItem2(body, lastMapId);
                    await mainForm.Map_Packet(tag, body, lastMapId);
                    break;
                
                case "item_summon":
                    {
                        string[] temp = { "," };
                        String[] data2 = body.Split(temp, StringSplitOptions.RemoveEmptyEntries);

                        string target = data2[0];
                        string x = data2[1];
                        string y = data2[2];

                        if (!mainForm.UserByNameDict.ContainsKey(target)) return;
                        await mainForm.UserByNameDict[target].SendMessageWithTagAsync(tag, x + "," + y);
                    }
                    break;
                case "map_name":
                    {
                        await systemDB.SaveMapAsync(message);
                        string[] temp = { "," };
                        String[] data2 = body.Split(temp, StringSplitOptions.RemoveEmptyEntries);

                        mapName = data2[1];
                        int new_id = int.Parse(data2[0]);
                        await mainForm.editUserMapAsync(this, new_id);
                        lastMapId = new_id;

                        await HandleReqMonsterAsync(); // 몬스터 데이터 보내기
                        await HandleItemsAsync();// 아이템 데이터 보내기
                        await HandleReqNpcAsync();// npc 데이터 보내기
                    }
                    break;
                case "9":
                    await CloseClientAsync(true);
                    break;

                // 교환 관련
                case "trade_invite":
                    await tradeManager.InviteTradeAsync(body);
                    break;
                case "trade_addItem":
                    {
                        bool check = await tradeManager.AddItemAsync(ParseKeyValueData(body));
                        await SendMessageWithTagAsync("trade_addItem", check ? "1" : "0");
                        if (check) await tradeManager.AddItemToTraderAsync();
                        break;
                    }
                case "trade_removeItem":
                    await tradeManager.RemoveItemAsync(int.Parse(body));
                    break;
                case "trade_ready":
                    await tradeManager.ReadyTradeAsync();
                    break;
                case "trade_cancel":
                    await tradeManager.CancelTradeAsync();
                    break;
                case "trade_accept":
                    await tradeManager.AcceptTradeAsync();
                    break;
                case "trade_refuse":
                    await tradeManager.RefuseTradeAsync();
                    break;

                // 파티 관련
                case "party_create":
                    await partyManger.CreatePartyAsync();
                    break;

                case "party_end":
                    await partyManger.EndPartyAsync();
                    break;

                case "party_invite":
                    {
                        Dictionary<string, string> dict = ParseKeyValueData(body);
                        string target = dict["target"];
                        await partyManger.InvitePartyAsync(target);
                    }
                    break;

                case "party_accept":
                    await partyManger.AcceptPartyAsync();
                    break;

                case "party_refuse":
                    await partyManger.RefusePartyAsync();
                    break;

                case "party_switch":
                    {
                        // 스위치 id, 스위치 상태, 맵 id
                        //string msg = string.Join(",", data);
                        await SendMessageWithTagAsync(tag, body); // 자신한테 보냄
                        await SendMessageToPartyMembersAsync(
                            (member) => (member.lastMapId == this.lastMapId) && (member != this),
                            (member) => member.SendMessageWithTagAsync(tag, body)
                            );
                    }
                    break;
                case "party_quest_check":
                    {
                        int map_id;
                        if (!int.TryParse(body, out map_id)) return;
                        
                        int[] check = systemData.checkPartyQuest(map_id);
                        if (check[0] == 0) return;

                        await SendMessageWithTagAsync(tag, check[0] + "," + check[1]);
                    }
                    break;
                case "party_move":
                    {
                        await SendMessageToPartyMembersAsync(
                            (member) => member.lastMapId == this.lastMapId,
                            (member) => member.SendMessageWithTagAsync(tag, body)
                            ); 
                    }
                    break;
                case "party_message":
                    {
                        var dict = ParseKeyValueData(body);
                        string className = dict["class"];
                        string text = dict["text"];
                        string msg = $"(파티) {userName}({className}) : {text}";

                        await SendMessageToPartyMembersAsync(
                            (member) => true,
                            (member) => member.SendMessageWithTagAsync(tag, msg)
                            );
                    }
                    break;
                case "party_heal":
                    {
                        var dict = ParseKeyValueData(body);
                        string id = dict["id"];
                        string value = dict["value"];
                        string msg = $"{userName} {id} {value}";

                        await SendMessageToPartyMembersAsync(
                            (member) => !member.Equals(this) && (member.lastMapId == this.lastMapId),
                            (member) => member.SendMessageWithTagAsync(tag, msg)
                            );
                    }
                    break;

                case "party_gain":
                    await SendMessageToPartyMembersAsync(
                           (member) => !member.Equals(this) && (member.lastMapId == this.lastMapId),
                           (member) => member.SendMessageWithTagAsync(tag, body)
                           );
                    break;


                case "ship_time_check":
                    await SendMessageWithTagAsync(tag, mainForm.now_ship_target().ToString());
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
                            await SendMessageWithTagAsync(tag, "귓속말 할 상대가 없습니다.");
                            return;
                        }

                        string msg = $"(귓속말) {userName} : {ms}";

                        await mainForm.UserByNameDict[target].SendMessageWithTagAsync(tag, msg);
                        ms = $"{userName}->{target}:{ms}";
                        mainForm.write_log(ms);
                    }
                    break;
                default: await HandleDefaultAsync(tag, body); break;
            }
        }
        private async Task HandleDefaultAsync(string tag, string body)
        {
            string check = "<" + tag + ">";
            if (systemData.packetMessageDict.TryGetValue(check, out int packetValue))
            {
                if (packetValue == 0)
                    await mainForm.Packet(tag, body, userCode);
                else
                    await mainForm.Packet(tag, body);
            }
            else if (systemData.mapPacketMessageDict.TryGetValue(check, out int mapPacketValue))
            {
                if (mapPacketValue == 0)
                    await mainForm.Map_Packet(tag, body, lastMapId, userCode);
                else
                    await mainForm.Map_Packet(tag, body, lastMapId);
            }
        }

        private async void HandleImportantLog(string body)
        {
            var data = ParseKeyValueData(body);
            var file_name = data["tag"];
            var msg = data["body"];
            await Task.Run(() => mainForm.write_log_user(userName, msg, file_name));
        }

        private async Task HandleLoginAsync(string tag, string body)
        {
            string loginResult = await systemDB.LoginAsync(body);
            string[] loginData = loginResult.Split(',');
            int resultCode = int.Parse(loginData[2]);

            bool isDuplicate = mainForm.Checkid(loginData[1]);

            if (isDuplicate)
            {
                await SendMessageWithTagAsync(tag, "al,1"); // Already logged in
                return;
            }

            if (resultCode == 0) await SendMessageWithTagAsync(tag, "wu,1"); // Wrong username
            else if (resultCode == 1) await SendMessageWithTagAsync(tag, "wp,1"); // Wrong password
            else if (resultCode == 2) await CompleteLoginAsync(loginData);
        }

        private async Task CompleteLoginAsync(string[] loginData)
        {
            if (mainForm.UserByNameDict.Count > mainForm.max_user_name)
            {
                await SendMessageWithTagAsync("over", "서버 유저 수 제한입니다. 다음에 시도해주세요.");
                return;
            }

            if (mainForm.IsIPBanned(ipAddress))
            {
                await SendMessageWithTagAsync("over", "현재 차단된 IP입니다.");
                return;
            }

            userName = loginData[0];
            userId = loginData[1];
            mainForm.UserByNameDict[userName] = this;

            await systemDB.SaveLoginDate(userId);
            await SendMessageWithTagAsync("login", "allow," + userName);
            await SendMessageWithTagAsync("server_msg", "흑부엉의 바람의나라에 오신 것을 환영합니다.");
            if (mainForm.isTest) await SendMessageWithTagAsync("server_msg", "현재 테스트 서버입니다.");
        }

        private async Task HandleVersionCheck(string tag, string body)
        {
            if (body != mainForm.version)
            {
                await SendMessageWithTagAsync("over", "버전이 다릅니다.");
            }
            else
            {
                mainForm.write_log("카운트다운 시작");
                versionCheckTimer.Start();
                await SendMessageWithTagAsync(tag, mainForm.version); // 5초 내에 응답하지 않으면 퇴출
                await SendMessageWithTagAsync("timer_v", "");
            }
        }

        private async Task HandleEventAsync(string tag, string body)
        {
            double num;
            switch (tag)
            {
                case "exp_event":
                    if (mainForm.exe_event > 0) await SendMessageWithTagAsync(tag, mainForm.exe_event.ToString());
                    break;
                case "drop_event":
                    if (mainForm.drop_event > 0) await SendMessageWithTagAsync(tag, mainForm.drop_event.ToString());
                    break;
                case "exp_event_change":
                    if (double.TryParse(body, out num))
                        await mainForm.exe_event_sendAsync(num);
                    break;
                case "drop_event_change":
                    if (double.TryParse(body, out num))
                        await mainForm.drop_event_sendAsync(num);
                    break;
            }
        }

        private async Task HandleReqMonsterAsync()
        {
            if (systemData.party_quest_map_id.ContainsKey(lastMapId)) return; // No need to send monsters for party quest maps

            string tag = "req_monster";
            if (systemData.monster_data.ContainsKey(lastMapId))
            {
                await SendDataWithTagAsync(systemData.monster_data[lastMapId].Values, tag); // Use await to ensure data is sent asynchronously
            }
            else
            {
                await SendMessageWithTagAsync(tag, $"map_id:{lastMapId}");
            }
        }

        private async Task HandleItemsAsync()
        {
            string tag = "Drop";
            if (systemData.item_data2.ContainsKey(lastMapId))
            {
                await SendDataWithTagAsync(systemData.item_data2[lastMapId], tag); // Use await to ensure all items are sent asynchronously
            }
        }

        private async Task HandleReqNpcAsync()
        {
            string tag = "npc_create";
            if (systemData.npc_data.ContainsKey(lastMapId))
            {
                await SendDataWithTagAsync(systemData.npc_data[lastMapId].Values, tag); // Use await to ensure NPCs are sent asynchronously
            }
        }

        private async Task SendDataWithTagAsync<T>(IEnumerable<T> data, string tag)
        {
            foreach (var item in data)
            {
                string msg = ConvertObjectToMessage(item);
                await SendMessageWithTagAsync(tag, msg); // Await each message being sent
            }
        }

        private string ConvertObjectToMessage(object obj)
        {
            Type type = obj.GetType();
            var msgBuilder = new StringBuilder();

            foreach (var field in type.GetFields())
            {
                object value = field.GetValue(obj);
                msgBuilder.Append($"{field.Name}:{value}|");
            }

            return msgBuilder.ToString();
        }

        public async Task SendMessageWithTagAsync(string tag, string body = "")
        {
            // SW가 null이거나, 스트림이 닫혀 있으면 메시지를 보내지 않음
            if (this.SW == null || !this.SW.BaseStream.CanWrite)
            {
                return;
            }

            if (mainForm.print_chat_tag.Contains(tag))
                body = mainForm.abuse_filtering(body);

            string startTag = "<" + tag + ">";
            string endTag = "</" + tag + ">";
            string message = startTag + body + endTag;

            await this.SW.WriteLineAsync(message); // 비동기식으로 메시지 쓰기
            await this.SW.FlushAsync(); // 비동기식으로 플러시
        }

        public async Task SendConsoleMessageAsync(string body)
        {
            await SendMessageWithTagAsync("console_msg", body);
        }

        
        private async Task SendMessageToPartyMembersAsync(Func<UserThread, bool> condition, Func<UserThread, Task> action)
        {
            foreach (var member in partyManger.partyMembers)
            {
                if (member == null) continue;

                if (condition(member))
                {
                    await action(member); // Await the action performed for each member
                }
            }
        }

        public async Task CloseClientAsync(bool closeSwitch = false)
        {
            try
            {
                if (client.Connected && !closeSwitch)
                {
                    return;
                }

                if (userCode != "close") // Ensure the user isn't already closed
                {
                    await mainForm.removethread(this); // Remove thread safely
                }
                userCode = "close";

                // Close streams and network resources asynchronously
                SW?.Close();
                SR?.Close();
                NS?.Close();
                client?.Close();
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

            return data.Substring(startIndex, Math.Max(endIndex - startIndex, 0));
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
