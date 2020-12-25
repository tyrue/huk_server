using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using Org.BouncyCastle.Utilities;

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
        systemdata sd;
        List<String> plist;
        public Thread thread = null;

        public int last_map_id = 0;
        public String map_name = "";
        System_DB system_db;

        string[] ignore_ms =
        {
            "<mon_move",
            "<aggro",
            "<mon_damage",
            "<player_damage",
            "<enemy_dead",
            "<monster",

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
            "<27"
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
                                    SW.WriteLine("<user_limit>서버 유저 수 제한입니다. 다음에 시도해주세요.</user_limit>"); // 메시지 보내기
                                    SW.Flush();
                                    continue;
                                }

                                SW.WriteLine("<login>allow," + words[0] + "</login>");

                                // Set UserName, UserId
                                UserName = words[0];
                                UserId = words[1];
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
                                //MessageBox.Show();
                            }
                        }

                        // 경험치 이벤트 확인
                        else if (GetMessage.Contains("<exp_event>"))
                        {
                            int n = 0;
                            n = mainform.radioSelected();
                            if (mainform.exe_event > 0) n = mainform.exe_event;
                            SW.WriteLine("<exp_event>" + n + "</exp_event>");
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
                            List<Monster> da = sd.monster_data[last_map_id];
                            foreach (var d in da)
                            {
                                string s = d.map_id + "," + d.id + "," + d.hp + "," + d.x + "," + d.y + "," + d.direction + "," + d.respawn;
                                SW.WriteLine("<req_monster>" + s + "</req_monster>");
                            }
                            SW.Flush();
                        }

                        // DB에 아이템 데이터 저장
                        else if (GetMessage.Contains("<map_item>"))
                        {
                            sd.SaveItem(splitTag("map_item", GetMessage));
                        }

                        // DB에 아이템 데이터 삭제
                        else if (GetMessage.Contains("<del_item>"))
                        {
                            sd.DelItem(splitTag("del_item", GetMessage));
                        }

                        // 현재 맵의 아이템 정보 전달
                        else if (GetMessage.Contains("<req_item>"))
                        {
                            if (!sd.item_data.ContainsKey(last_map_id)) continue;
                            List<Item> da = sd.item_data[last_map_id];
                            foreach (var d in da)
                            {
                                SW.WriteLine("<drop_create>" + d.map_id + "," + d.id + "," + d.x + "," + d.y + "</drop_create>");
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
                            mainform.removeMapUser(last_map_id, this);
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
                        if(!client.Connected)
                        {
                            SW.Close();
                            SR.Close();
                            client.Close();
                            NS.Close();
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
