using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SupremePlayServer
{
    public partial class MainForm : Form
    {
        public List<UserThread> UserList;

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Initialize
            UserList = new List<UserThread>();

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
                }
            }
            catch (Exception e)
            {

            }
            finally
            {
                
            }
        }

        #endregion

        // 모든 유저에게 전송하는 패킷
        public void Packet(String data)
        {
            for (int i = 0; i < UserList.Count; i++)
            {
                if (!UserList[i].UserCode.Equals("*null*"))
                {
                    try
                    {
                        MessageBox.Show(data);
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
            PlayerCount();

            if(data.Contains("<chat1>"))
            {
                if (listBox2.Items.Count <= 300) // 서버 채팅 메세지 목록 개수 제한
                {
                    string[] word = splitTag("chat1", data).Split(',');
                    listBox2.Items.Add(word[0]);
                    int visibleItems = listBox2.ClientSize.Height / listBox2.ItemHeight;
                    listBox2.TopIndex = Math.Max(listBox2.Items.Count - visibleItems + 1, 0);
                }
                else
                    listBox2.Items.Clear();
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
                    if (userthread.UserName != null)
                    {
                        UserList.Remove(userthread); 
                        PlayerCount();
                        Packet("<chat>(알림): '" + userthread.UserName + "'님께서 게임을 종료하셨습니다.</chat>");
                    }

                    if (userthread.thread != null)
                    {
                        UserList.Remove(userthread); // 여기서 문제인건데...
                        PlayerCount();
                        userthread.thread.Abort();
                    }
                }
            }
            catch (Exception e)
            {
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
            }

            return check;
        }

        private void FormClose(object sender, FormClosedEventArgs e)
        {
            Application.ExitThread();
            Environment.Exit(0);
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        private void PlayerCount()
        {
            label_playercount.Text = "접속자 수 : " + UserList.Count;

            listBox1.Items.Clear();
            for(int i=0; i< UserList.Count; i++)
            {
                listBox1.Items.Add(UserList[i].UserName + "(" + UserList[i].UserId + ")");
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
            if (!textBox1.Text.Equals(""))
            {
                if(comboBox1.SelectedIndex == 0) // 공지
                {
                    Packet("<chat>" + textBox1.Text + "</chat>");
                    listBox2.Items.Add(textBox1.Text);
                    textBox1.Text = "";
                }
                else if (comboBox1.SelectedIndex == 1) // 감옥
                {
                    if(listBox1.SelectedIndex >= 0)
                    {
                        MessageBox.Show(listBox1.Items[listBox1.SelectedIndex].ToString());
                        Packet("<prison>" + listBox1.Items[listBox1.SelectedIndex] + "</prison>");
                        listBox2.Items.Add(listBox1.Items[listBox1.SelectedIndex] + " 감옥");
                        textBox1.Text = "";
                    }
                }
                else if (comboBox1.SelectedIndex == 2) // 석방
                {
                    if (listBox1.SelectedIndex >= 0)
                    {
                        MessageBox.Show(listBox1.Items[listBox1.SelectedIndex].ToString());
                        Packet("<emancipation>" + listBox1.Items[listBox1.SelectedIndex] + "</emancipation>");
                        listBox2.Items.Add(listBox1.Items[listBox1.SelectedIndex] + " 석방");
                        textBox1.Text = "";
                    }
                }
                else if (comboBox1.SelectedIndex == 3) // 유저 강퇴
                {
                    if (listBox1.SelectedIndex >= 0)
                    {
                        MessageBox.Show(listBox1.Items[listBox1.SelectedIndex].ToString());
                        Packet("<ki>" + listBox1.Items[listBox1.SelectedIndex] + "," + textBox1.Text + ",</ki>");
                        listBox2.Items.Add(textBox1.Text);
                        textBox1.Text = "";
                    }
                }
                else if (comboBox1.SelectedIndex == 4) // 모두 강퇴
                {
                    Packet("<ki>모두," + textBox1.Text + ",</ki>");
                    listBox2.Items.Add(textBox1.Text);
                    textBox1.Text = "";
                }
            }
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }
    }

}
