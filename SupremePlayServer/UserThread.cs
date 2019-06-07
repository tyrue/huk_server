using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net.Sockets;
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
        systemdata sd = new systemdata();
        List<String> plist;
        public Thread thread = null;

        String last_map_id = "0";
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
                        else if (GetMessage.Contains("<nickname>"))
                        {
                            System_DB system_db = new System_DB();
                            system_db.Registeration(NS, GetMessage);
                        }

                        // Login
                        else if (GetMessage.Contains("<login"))
                        {
                            System_DB system_db = new System_DB();
                            String Ldata = system_db.Login(GetMessage);

                            String[] words = Ldata.Split(',');
                            int resultcode = Int32.Parse(words[2]);

                            bool existconn = false;
                            mainform.Invoke((MethodInvoker)(() => existconn = mainform.Checkid(words[0])));

                            if (existconn) resultcode = 3;

                            // 아이디 잘못 입력
                            if (resultcode == 0)
                                SW.WriteLine("<login>wu</login>");

                            // 비번 잘못입력
                            else if (resultcode == 1)
                                SW.WriteLine("<login>wp</login>");

                            // 로긴 성공
                            else if (resultcode == 2)
                            {
                                SW.WriteLine("<login>allow</login>");

                                // Set UserName, UserId
                                UserName = words[0];
                                UserId = words[1];
                            }

                            // 이미 접속중
                            else if (resultcode == 3)
                                SW.WriteLine("<login>al</login>");

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

                        // 현재 맵에서의 기준이 될 유저 확인
                        else if (GetMessage.Contains("<map_player>"))
                        {
                            System_DB system = new System_DB();
                            String map_id = system.splitTag("map_player", GetMessage);
                            if (!mainform.MapUser.ContainsKey(map_id))
                            {
                                List<String> a = new List<string>
                                {
                                    UserCode
                                };
                                mainform.MapUser.Add(map_id, a);
                                SW.WriteLine("<map_player>1</map_player>");
                                SW.Flush();
                            }
                            else if (mainform.MapUser[map_id].Count == 0)
                            {
                                mainform.MapUser[map_id].Add(UserCode);
                                SW.WriteLine("<map_player>1</map_player>");
                                SW.Flush();
                            }
                            else
                            {
                                if(!mainform.MapUser[map_id].Contains(UserCode))
                                {
                                    mainform.MapUser[map_id].Add(UserCode);
                                    SW.WriteLine("<map_player>0</map_player>");
                                    SW.Flush();
                                }
                            }

                            if(mainform.MapUser.ContainsKey(last_map_id))
                            {
                                if(mainform.MapUser[last_map_id].Contains(UserCode))
                                {
                                    mainform.MapUser[last_map_id].Remove(UserCode);

                                    if(mainform.MapUser[last_map_id].Count != 0)
                                    {
                                        for (int i = 0; i < mainform.UserList.Count; i++)
                                        {
                                            if (mainform.UserList[i].UserCode.Equals(mainform.MapUser[last_map_id][0]))
                                            {
                                                try
                                                {
                                                    mainform.UserList[i].SW.WriteLine("<map_player>1</map_player>"); // 메시지 보내기
                                                    mainform.UserList[i].SW.Flush();
                                                    break;
                                                }
                                                catch (Exception e) // 팅긴걸로 판단
                                                {

                                                }
                                            }
                                            // 유효하지 않은 유저는 삭제
                                            else
                                            {

                                            }
                                        }
                                    }
                                }
                            }
                            last_map_id = map_id;
                        }

                        // 유저 종료
                        else if (GetMessage.Contains("<9>"))
                        {
                            if (mainform.MapUser.ContainsKey(last_map_id))
                            {
                                if (mainform.MapUser[last_map_id].Contains(UserCode))
                                {
                                    mainform.MapUser[last_map_id].Remove(UserCode);

                                    if (mainform.MapUser[last_map_id].Count != 0)
                                    {
                                        for (int i = 0; i < mainform.UserList.Count; i++)
                                        {
                                            if (mainform.UserList[i].UserCode.Equals(mainform.MapUser[last_map_id][0]))
                                            {
                                                try
                                                {
                                                    mainform.UserList[i].SW.WriteLine("<map_player>1</map_player>"); // 메시지 보내기
                                                    mainform.UserList[i].SW.Flush();
                                                    break;
                                                }
                                                catch (Exception e) // 팅긴걸로 판단
                                                {

                                                }
                                            }
                                            // 유효하지 않은 유저는 삭제
                                            else
                                            {

                                            }
                                        }
                                    }
                                }
                            }

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
                                if(d1[0].Contains("mon_move"))
                                    mainform.Invoke((MethodInvoker)(() => mainform.Packet(GetMessage, UserCode)));
                                else
                                    mainform.Invoke((MethodInvoker)(() => mainform.Packet(GetMessage)));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {

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
