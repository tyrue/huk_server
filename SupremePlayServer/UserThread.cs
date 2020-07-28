using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.Linq;

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
        systemdata sd = new systemdata();
        List<String> plist;
        public Thread thread = null;

        public int last_map_id = 0;
        public String map_name = "";

        string[] map_message = 
        {
            "mon_move",
            "aggro",
            "mon_damage",
            "player_damage",
            "enemy_dead",
            "nptgain",
            "partyhill"
        };
        public void startClient(TcpClient clientSocket)
        {
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
            try
            {
                while (true)
                {
                    while (client.Connected) //클라이언트 메시지받기
                    {
                        GetMessage = SR.ReadLine();

                        // Log
                        /*
                        if (mainform != null)
                            mainform.label1.Invoke((MethodInvoker)(() => mainform.label1.Text += GetMessage + "\n"));
                         * */

                        // Authorization 인증
                        if (GetMessage.Contains("<0>"))
                        {
                            SW.WriteLine("<0 " + UserCode + ">'e' n=Suprememay Server</0>"); // 메시지 보내기
                            SW.Flush();
                        }

                        // Registration
                        else if (GetMessage.Contains("<regist>"))
                        {
                            System_DB system_db = new System_DB();
                            system_db.Registeration(NS, GetMessage);
                        }

                        // Login
                        else if (GetMessage.Contains("<login"))
                        {
                            System_DB system_db = new System_DB();
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
                            System_DB system_db = new System_DB();
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
                                MessageBox.Show(e.ToString());
                            }
                        }

                        // 경험치 이벤트 확인
                        else if (GetMessage.Contains("<exp_event>"))
                        {
                            SW.WriteLine("<exp_event>" + mainform.radioSelected().ToString() + "</exp_event>");
                            SW.Flush();
                        }

                        // 현재 유저의 정보를 모든 유저에게 보냄
                        else if (GetMessage.Contains("<5>"))
                        {
                            string[] co1 = { "<5>" };
                            String[] d1 = GetMessage.Split(co1, StringSplitOptions.RemoveEmptyEntries);

                            mainform.Invoke((MethodInvoker)(() => mainform.Packet("<5 " + UserCode + ">" + d1[0])));
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
                            System_DB system_db = new System_DB();
                            system_db.SendData(NS, UserId);
                        }

                        // 몬스터 데이터 저장
                        else if (GetMessage.Contains("<monster>"))
                        {
                            //MessageBox.Show("몬스터 정보 저장");
                            System_DB system_db = new System_DB();
                            system_db.SaveMonster(GetMessage);
                        }

                        // 몬스터 데이터 로드
                        else if (GetMessage.Contains("<req_monster>"))
                        {
                            //MessageBox.Show("몬스터 정보 요청");
                            System_DB system_db = new System_DB();
                            system_db.SendMonster(NS, GetMessage);
                        }

                        // DB에 아이템 데이터 저장
                        else if (GetMessage.Contains("<map_item>"))
                        {
                            System_DB system_db = new System_DB();
                            system_db.SaveItem(GetMessage);
                        }

                        // DB에 아이템 데이터 삭제
                        else if (GetMessage.Contains("<del_item>"))
                        {
                            System_DB system_db = new System_DB();
                            system_db.DelItem(GetMessage);
                        }

                        // 현재 맵의 아이템 정보 전달
                        else if (GetMessage.Contains("<req_item>"))
                        {
                            System_DB system_db = new System_DB();
                            system_db.SendItem(NS, GetMessage);
                        }

                        // 유저가 맵을 옮김 -> 바뀐 맵에서 기준이 되는지 확인
                        // 현재 맵 이름 저장
                        else if (GetMessage.Contains("<map_name>"))
                        {
                            System_DB system = new System_DB();
                            system.SaveMap(GetMessage);

                            string data = system.splitTag("map_name", GetMessage);
                            string[] co1 = { "," };
                            String[] data2 = data.Split(co1, StringSplitOptions.RemoveEmptyEntries);

                            int map_id = int.Parse(data2[0]);
                            if(!mainform.MapUser2.ContainsKey(map_id)) // 해당 맵에 아무도 없었다면?
                            {
                                mainform.MapUser2.Add(map_id, new List<UserThread>());
                                SW.WriteLine("<map_player>1</map_player>");
                                SW.Flush();
                            }
                            else if(mainform.MapUser2[map_id].Count == 0)
                            {
                                SW.WriteLine("<map_player>1</map_player>");
                            }
                            else
                            {
                                SW.WriteLine("<map_player>0</map_player>");
                            }
                            mainform.MapUser2[map_id].Add(this);

                            // 이전에 있었던 리스트에서 제거함
                            mainform.removeMapUser(last_map_id, this);
                            last_map_id = map_id;
                            map_name = system.SendMap(last_map_id);

                            mainform.PlayerCount();
                        }


                        // 유저 종료
                        else if (GetMessage.Contains("<9>"))
                        {
                            mainform.removeMapUser(last_map_id, this);
                            System_DB system = new System_DB();
                            if (!UserCode.Equals("*null*") && system.splitTag("9", GetMessage).Equals(UserCode))
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
                                    mainform.Invoke((MethodInvoker)(() => mainform.Map_Packet(GetMessage, last_map_id, UserCode)));
                                else
                                    mainform.Invoke((MethodInvoker)(() => mainform.Packet(GetMessage, UserCode)));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //MessageBox.Show(e.ToString());
            }
            finally
            {
                SW.Close();
                SR.Close();
                client.Close();
                NS.Close();
            }
        }
    }
}
