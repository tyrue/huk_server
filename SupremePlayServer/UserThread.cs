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
        NetworkStream NS = null;
        StreamReader SR = null;
        public StreamWriter SW = null;
        public TcpClient client;

        public String UserCode = "*null*";
        public MainForm mainform;

        // User Data
        public String UserId;
        public String UserName;

        // Get Packet List
        Systemdata sd;
        List<String> plist;
        public Thread thread = null;

        public int last_map_id = 0;
        public String map_name = "";
        System_DB system_db;
        bool is_v = false;

        // 타이머 생성 및 시작
        System.Timers.Timer timer2;

        string[] ignore_ms =
        {
            "<mon_move",
            "<aggro",
            "<mon_damage",
            "<player_damage",
            "<enemy_dead",
            "<monster",
            "<hp",
            "<drop_del",
            "<del_item",
            "<drop_create",
            "<userdata",
        };

        string[] map_message =
        {
            "<mon_move",
            "<aggro",
            "<mon_damage",
            "<player_damage",
            "<enemy_dead",
            "<nptgain",
            "<partyhill",
            "<npt_move",
            "<map_chat",
            "<27",
            "<show_range_skill",
            "<hp",
            "<monster",
            "<drop_del",
            "<del_item",
            "<drop_create",
            "<attack_effect",
            "<skill_effectm",
            "<monster_chat",
        };

        public void startClient(TcpClient clientSocket)
        {
            sd = mainform.sd;
            system_db = mainform.system_db;
            // Get Packet List
            plist = sd.getAllpacketList();

            // Get UserCode Randomly
            Random random = new Random();
            int randval = random.Next(0, 9999999);
            UserCode = randval.ToString();

            // Create Client Socket & Thread
            client = clientSocket;
            thread = new Thread(NetListener);
            thread.IsBackground = true;
            thread.Start();

            timer2 = new System.Timers.Timer();
            timer2.Interval = 1000;
            timer2.Elapsed += new System.Timers.ElapsedEventHandler(timer_tick);
        }

        void timer_tick(object sender, EventArgs e)
        {
            try
            {
                mainform.write_log("카운트다운 끝");
                if (!is_v)
                {
                    mainform.write_log("version_false");
                    SW.WriteLine("<over>버전이 다릅니다.</over>");
                    SW.Close();
                    SR.Close();
                    client.Close();
                    NS.Close();
                }
                else
                {
                    mainform.write_log("version_true");
                }
            }
            catch
            {
                mainform.write_log(e.ToString());
            }
            timer2.Stop();
        }

        // Thread - Net Listener
        public void NetListener()
        {
            NS = client.GetStream(); // 소켓에서 메시지를 가져오는 스트림
            SR = new StreamReader(NS, Encoding.UTF8); // Get message
            SW = new StreamWriter(NS, Encoding.UTF8); // Send message

            string GetMessage = string.Empty;
            while (true)
            {
                while (client.Connected) //클라이언트 메시지받기
                {
                    try
                    {
                        GetMessage = SR.ReadLine();
                        if (GetMessage == null) continue;
                        // Log
                        /*
                        if (mainform != null)
                            mainform.label1.Invoke((MethodInvoker)(() => mainform.label1.Text += GetMessage + "\n"));
                         * */
                        //MessageBox.Show(GetMessage);
                        // Authorization 인증
                        if (UserName != "" || UserName != null || UserName.Length != 0)
                        {
                            string[] co1 = { ">" };
                            String[] d1 = GetMessage.Split(co1, StringSplitOptions.RemoveEmptyEntries);
                            if (!ignore_ms.Contains(d1[0]))
                                mainform.write_log_user(UserName, GetMessage);
                        }


                        if (GetMessage.Contains("<0>"))
                        {
                            SW.WriteLine("<0 " + UserCode + ">'e' n=Suprememay Server</0>"); // 메시지 보내기
                            SW.Flush();
                        }

                        // Registration
                        else if (GetMessage.Contains("<regist>"))
                        {
                            system_db.Registeration(NS, GetMessage);
                        }

                        // Login
                        else if (GetMessage.Contains("<login"))
                        {
                            String Ldata = system_db.Login(GetMessage); // 로그인 결과 받아옴

                            String[] words = Ldata.Split(',');
                            int resultcode = Int32.Parse(words[2]);

                            bool existconn = false;
                            mainform.Invoke((MethodInvoker)(() => existconn = mainform.Checkid(words[1])));

                            if (existconn) resultcode = 3;

                            // 아이디 잘못 입력
                            if (resultcode == 0)
                                SW.WriteLine("<login>wu,1</login>");

                            // 비번 잘못입력
                            else if (resultcode == 1)
                                SW.WriteLine("<login>wp,1</login>");

                            // 로긴 성공
                            else if (resultcode == 2)
                            {
                                if (mainform.UserList.Count > mainform.max_user_name)
                                {
                                    SW.WriteLine("<sever_msg>서버 유저 수 제한입니다. 다음에 시도해주세요.</sever_msg>"); // 메시지 보내기
                                    SW.Flush();
                                    continue;
                                }

                                SW.WriteLine("<login>allow," + words[0] + "</login>");

                                // Set UserName, UserId
                                UserName = words[0];
                                UserId = words[1];
                                mainform.UserByNameDict.Add(UserName, this);
                                SW.WriteLine("<sever_msg>흑부엉의 바람의나라에 오신것을 환영합니다.</sever_msg>");
                            }

                            // 이미 접속중
                            else if (resultcode == 3)
                                SW.WriteLine("<login>al,1</login>");

                            SW.Flush();
                        }

                        else if (GetMessage.Contains("<2>"))
                        {
                            SW.WriteLine("<2>" + UserId + "</2>");
                            SW.Flush();
                        }

                        else if (GetMessage.Contains("<check>"))
                        {
                            SW.WriteLine("<check>standard</check>");
                            SW.Flush();
                        }

                        else if (GetMessage.Contains("<versione>"))
                        {
                            string ver = splitTag("versione", GetMessage);
                            if (ver != mainform.version)
                            {
                                SW.WriteLine("<over>버전이 다릅니다.</over>");
                                SW.WriteLine("<versione>" + mainform.version + "</versione>");
                                SW.Close();
                                SR.Close();
                                client.Close();
                                NS.Close();
                                return;
                            }
                            else
                            {
                                mainform.write_log("카운트다운 시작");
                                SW.WriteLine("<versione>" + mainform.version + "</versione>");
                                timer2.Start();
                                // 여기서부터 5초안에 타이머 켜서 만약 원하는 답을 주지 않을 경우 퇴출 시켜버림
                                SW.WriteLine("<timer_v></timer_v>");
                            }
                            SW.Flush();
                        }

                        else if (GetMessage.Contains("<timer_v>"))
                        {
                            string ver = splitTag("timer_v", GetMessage);
                            if (ver == "ok")
                            {
                                is_v = true;
                            }
                        }


                        // 유저 데이터 저장
                        else if (GetMessage.Contains("<userdata>"))
                        {
                            system_db.SaveData(GetMessage, UserId);

                            try
                            {
                                if (UserCode.Equals("*null*"))
                                {
                                    mainform.Invoke((MethodInvoker)(() => mainform.removethread(this)));
                                    thread.Abort();
                                }
                            }
                            catch (Exception e)
                            {
                                mainform.write_log(e.ToString());
                                //MessageBox.Show();
                            }
                        }

                        // 경험치 이벤트 확인
                        else if (GetMessage.Contains("<exp_event>"))
                        {
                            int n = 0;
                            if (mainform.exe_event > 0) n = mainform.exe_event;
                            SW.WriteLine("<exp_event>" + n + "</exp_event>");
                            SW.Flush();
                        }

                        // 드랍율 이벤트 확인
                        else if (GetMessage.Contains("<drop_event>"))
                        {
                            double n = 0;
                            if (mainform.exe_event > 0) n = mainform.drop_event;
                            SW.WriteLine("<drop_event>" + n + "</drop_event>");
                            SW.Flush();
                        }

                        // 현재 유저의 정보를 모든 유저에게 보냄
                        else if (GetMessage.Contains("<chat>"))
                        {
                            string[] co1 = { "<chat>" };
                            String[] d1 = GetMessage.Split(co1, StringSplitOptions.RemoveEmptyEntries);

                            mainform.Invoke((MethodInvoker)(() => mainform.Packet("<chat>" + d1[0])));
                        }

                        // 현재 유저의 정보를 모든 유저에게 보냄
                        else if (GetMessage.Contains("<5>"))
                        {
                            string[] co1 = { "<5>" };
                            String[] d1 = GetMessage.Split(co1, StringSplitOptions.RemoveEmptyEntries);

                            mainform.Invoke((MethodInvoker)(() => mainform.Packet("<5 " + UserCode + ">" + d1[0], UserCode)));
                        }

                        // 현재 유저의 정보를 같은 맵 유저에게 보냄
                        else if (GetMessage.Contains("<m5>"))
                        {
                            string[] co1 = { "<m5>" };
                            String[] d1 = GetMessage.Split(co1, StringSplitOptions.RemoveEmptyEntries);

                            mainform.Invoke((MethodInvoker)(() => mainform.Map_Packet("<5 " + UserCode + ">" + d1[0], last_map_id, UserCode)));
                        }

                        // 유저 데이터 로드
                        else if (GetMessage.Contains("<dtloadreq>"))
                        {
                            system_db.SendData(NS, UserId);
                        }

                        // 몬스터 데이터 저장
                        else if (GetMessage.Contains("<monster>"))
                        {
                            sd.SaveMonster(splitTag("monster", GetMessage));
                        }

                        // 몬스터 데이터 로드
                        else if (GetMessage.Contains("<req_monster>"))
                        {
                            //MessageBox.Show("몬스터 정보 요청");
                            if (!sd.monster_data.ContainsKey(last_map_id)) continue;
                            var da = sd.monster_data[last_map_id].Values;
                            foreach (var d in da)
                            {
                                string s = d.map_id + "," + d.id + "," + d.hp + "," + d.x + "," + d.y + "," + d.direction + "," + d.respawn;
                                SW.WriteLine("<req_monster>" + s + "</req_monster>");
                            }
                            SW.Flush();
                        }

                        // 몬스터 마력 공유 : id, val
                        else if (GetMessage.Contains("<monster_sp>"))
                        {
                            if (!sd.monster_data.ContainsKey(last_map_id)) continue;
                            string data = system_db.splitTag("monster_sp", GetMessage);
                            string[] co1 = { "," };
                            String[] data2 = data.Split(co1, StringSplitOptions.RemoveEmptyEntries);

                            int id = int.Parse(data2[0]);
                            int sp = int.Parse(data2[1]);

                            sd.monster_data[last_map_id][id].sp = sp;
                            mainform.Invoke((MethodInvoker)(() => mainform.Map_Packet(GetMessage, last_map_id, UserCode)));
                        }

                        // DB에 아이템 데이터 저장
                        else if (GetMessage.Contains("<Drop>"))
                        {
                            sd.SaveItem2(splitTag("Drop", GetMessage));
                            mainform.Invoke((MethodInvoker)(() => mainform.Map_Packet(GetMessage, last_map_id)));
                        }

                        // DB에 아이템 데이터 삭제
                        else if (GetMessage.Contains("<Drop_Get>"))
                        {
                            sd.DelItem2(splitTag("Drop_Get", GetMessage));
                            mainform.Invoke((MethodInvoker)(() => mainform.Map_Packet(GetMessage, last_map_id)));
                        }

                        // 현재 맵의 아이템 정보 전달
                        else if (GetMessage.Contains("<req_item>"))
                        {
                            if (!sd.item_data2.ContainsKey(last_map_id)) continue;
                            List<Item2> da = sd.item_data2[last_map_id];
                            foreach (var d in da)
                            {
                                SW.WriteLine("<Drop>" + d.d_id + "," + d.type2 + "," + d.type1 + "," + d.id + "," + d.map_id + "," + d.x + "," + d.y + "," + d.num + "</Drop>");
                            }

                            SW.Flush();
                        }

                        // 유저가 맵을 옮김 -> 바뀐 맵에서 기준이 되는지 확인
                        // 현재 맵 이름 저장
                        else if (GetMessage.Contains("<map_name>"))
                        {
                            system_db.SaveMap(GetMessage);

                            string data = system_db.splitTag("map_name", GetMessage);
                            string[] co1 = { "," };
                            String[] data2 = data.Split(co1, StringSplitOptions.RemoveEmptyEntries);

                            int map_id = int.Parse(data2[0]);
                            if (!mainform.MapUser2.ContainsKey(map_id)) // 해당 맵에 아무도 없었다면?
                            {
                                mainform.MapUser2.Add(map_id, new List<UserThread>());
                                SW.WriteLine("<map_player>1</map_player>");
                            }
                            else if (mainform.MapUser2[map_id].Count == 0)
                            {
                                SW.WriteLine("<map_player>1</map_player>");
                            }
                            else
                            {
                                SW.WriteLine("<map_player>0</map_player>");
                            }
                            SW.Flush();
                            mainform.MapUser2[map_id].Add(this);

                            // 이전에 있었던 리스트에서 제거함
                            mainform.removeMapUser(last_map_id, this);
                            last_map_id = map_id;
                            map_name = sd.SendMap(last_map_id);
                            mainform.PlayerCount();
                        }


                        // 유저 종료
                        else if (GetMessage.Contains("<9>"))
                        {
                            mainform.removethread(this);
                            if (!UserCode.Equals("*null*") && system_db.splitTag("9", GetMessage).Equals(UserCode))
                            {
                                if (UserName != null)
                                {
                                    mainform.Invoke((MethodInvoker)(() => mainform.Packet(GetMessage)));
                                    SW.Close();
                                    SR.Close();
                                    client.Close();
                                    NS.Close();
                                    mainform.Invoke((MethodInvoker)(() => mainform.Packet(GetMessage)));
                                }
                                else
                                {
                                    SW.Close();
                                    SR.Close();
                                    client.Close();
                                    NS.Close();
                                    return;
                                }
                                UserCode = "*null*";
                            }
                        }

                        else if (GetMessage.Contains("<party_switch>"))
                        {
                            // 스위치 id, 스위치 상태, 맵 id
                            string data = splitTag("party_switch", GetMessage);
                            string[] co1 = { "," };
                            String[] data2 = data.Split(co1, StringSplitOptions.RemoveEmptyEntries);
                            mainform.switch_send(data2[0], data2[1], int.Parse(data2[2]));
                        }

                        else if (GetMessage.Contains("<party_quest_check>"))
                        {
                            string data = splitTag("party_quest_check", GetMessage);
                            int map_id;
                            if (int.TryParse(data, out map_id))
                            {
                                try
                                {
                                    int[] check = mainform.sd.checkPartyQuest(map_id);
                                    if(check[0] != 0)
                                    {
                                        SW.WriteLine("<party_quest_check>" + check[0].ToString() + "," + check[1].ToString() + "</party_quest_check>");
                                        SW.Flush();
                                    }
                                }
                                catch (Exception e)
                                {
                                    mainform.write_log(e.ToString());
                                }
                            }
                        }

                        else if (GetMessage.Contains("<ship_time_check>"))
                        {
                            try
                            {
                                SW.WriteLine("<ship_time_check>" + mainform.now_ship_target() +"</ship_time_check>");
                                SW.Flush();
                            }
                            catch (Exception e)
                            {
                                mainform.write_log(e.ToString());
                            }
                        }

                        else if (GetMessage.Contains("<monster_cooltime_reset>"))
                        {
                            // 스위치 id, 스위치 상태, 맵 id
                            string data = splitTag("monster_cooltime_reset", GetMessage);
                            string[] co1 = { "," };
                            String[] data2 = data.Split(co1, StringSplitOptions.RemoveEmptyEntries);
                            if (data2.Length >= 2)
                                mainform.monster_cooltime_reset(int.Parse(data2[0]), int.Parse(data2[1]));
                            else
                                mainform.monster_cooltime_reset(int.Parse(data2[0]));
                        }

                        else if (GetMessage.Contains("<npt_move>"))
                        {
                            mainform.Invoke((MethodInvoker)(() => mainform.Map_Packet(GetMessage, last_map_id)));
                        }

                        else if (GetMessage.Contains("<whispers>"))
                        {
                            string data = splitTag("whispers", GetMessage);
                            string[] co1 = { "," };
                            String[] data2 = data.Split(co1, StringSplitOptions.RemoveEmptyEntries);

                            string target = data2[0];
                            string msg = data2[1];

                            if(!mainform.UserByNameDict.ContainsKey(target))
                            {
                                SW.WriteLine("<whispers>귓속말 할 상대가 없습니다.</whispers>");
                            }
                            else
                            {
                                mainform.UserByNameDict[target].SW.WriteLine("<whispers>" + UserName + " : " + msg + "</whispers>");
                                mainform.UserByNameDict[target].SW.Flush();
                                msg = UserName + " -> " + target + " : " + msg;
                                mainform.write_log(msg);
                            }
                            SW.Flush();
                        }

                        // 나머지는 다 방송함
                        else if (!GetMessage.Equals("null"))
                        {
                            string[] co1 = { ">" };
                            String[] d1 = GetMessage.Split(co1, StringSplitOptions.RemoveEmptyEntries);

                            if (plist.IndexOf(d1[0] + ">") != -1)
                            {
                                if (map_message.Contains(d1[0]))
                                {
                                    mainform.Invoke((MethodInvoker)(() => mainform.Map_Packet(GetMessage, last_map_id, UserCode)));
                                }
                                else
                                    mainform.Invoke((MethodInvoker)(() => mainform.Packet(GetMessage, UserCode)));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        //mainform.write_log(e.ToString());
                        //MessageBox.Show(e.ToString());
                        if (!client.Connected)
                        {
                            SW.Close();
                            SR.Close();
                            client.Close();
                            NS.Close();
                            mainform.removethread(this);
                            return;
                        }
                    }
                }
            }
        }

        public String splitTag(String tag, String data)
        {
            string[] co1 = { "<" + tag + ">" };
            String[] d1 = data.Split(co1, StringSplitOptions.RemoveEmptyEntries);

            string[] co2 = { "</" + tag + ">" };
            String[] d2 = d1[0].Split(co2, StringSplitOptions.RemoveEmptyEntries);

            return d2[0];
        }


    }
}
