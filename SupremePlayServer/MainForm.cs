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
                Listener = new TcpListener(Int32.Parse(Properties.Resources.PORT));
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
                        UserList[i].SW.WriteLine(data); // 메시지 보내기
                        UserList[i].SW.Flush();
                    }
                    catch (Exception e)
                    {
                        UserList[i].thread.Abort();
                        UserList.Remove(UserList[i]);
                        PlayerCount();
                    }
                }

                // 유효하지 않은 유저는 삭제
                else
                {
                    try
                    {
                        if (UserList[i].thread != null)
                            UserList[i].thread.Abort();
                        UserList.Remove(UserList[i]);
                        PlayerCount();
                    }
                    catch (Exception e) { }
                }

                try
                {
                    // 접속이 되지 않은 유저 삭제 : 중간에 팅긴 유저에 대한 처리
                    if (!UserList[i].client.Connected)
                    {
                        if (UserList[i].UserName != null)
                            Packet("<chat>(알림): '" + UserList[i].UserName + "'님께서 게임을 종료하셨습니다.</chat>");

                        if (UserList[i].thread != null)
                            UserList[i].thread.Abort();
                        UserList.Remove(UserList[i]);
                        PlayerCount();
                    }
                }
                catch (Exception e)
                {

                }
            }

            PlayerCount();

            if(data.Contains("<chat1>"))
            {
                if (listBox2.Items.Count <= 300)
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
                UserList.Remove(userthread);
                PlayerCount();
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
                Packet("<chat>" + textBox1.Text + "</chat>");
                listBox2.Items.Add(textBox1.Text);
                textBox1.Text = "";
            }
        }
    }

}
