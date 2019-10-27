using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace SupremePlayServer
{
    class System_DB : UserThread
    {
        private string DBInfo = "Server=127.0.0.1;Database=supremeplay;Uid=root;Pwd=abs753951;CharSet=utf8";

        #region 회원가입

        public void Registeration(NetworkStream NS, String data)
        {
            try
            {
                // resultcode  // 0 : 아이디 없음 1 : 아이디 이미 있음 2 : 닉네임 이미 있음
                int resultcode = 0;
                StreamWriter SW = new StreamWriter(NS, Encoding.UTF8);
                String[] d1;

                using (MySqlConnection conn = new MySqlConnection(DBInfo))
                {
                    // get Data
                    string[] co = { "," };
                    d1 = splitTag("regist", data).Split(co, StringSplitOptions.RemoveEmptyEntries);
                    
                    // DB Connection
                    conn.Open();
                    string sql = "SELECT* FROM user";

                    // Mysql Connection
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    MySqlDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        if (rdr["id"].ToString().Equals(d1[1]))
                        {
                            resultcode = 1;
                            break;
                        }
                        if (rdr["nickname"].ToString().Equals(d1[0]))
                        {
                            resultcode = 2;
                            break;
                        }
                    }
                    rdr.Close();
                    conn.Close();
                }

                // Send Message To Client
                if (NS != null)
                {
                    // No Exist nickname & id
                    if (resultcode == 0)
                    {
                        using (MySqlConnection conn = new MySqlConnection(DBInfo))
                        {
                            // DB Connection
                            conn.Open();

                            string sql = "INSERT INTO user VALUES('" + d1[0] + "', '" + d1[1] + "', '" + d1[2] + "', now())";
                            MySqlCommand cmd = new MySqlCommand(sql, conn);
                            cmd.ExecuteNonQuery();
                            
                            SW.WriteLine("<regist>success</regist>");
                        }
                    }

                    // Already Exist Id
                    else if (resultcode == 1)
                        SW.WriteLine("<regist>wi</regist>");

                    // Already Exist nickname
                    else if (resultcode == 2)
                        SW.WriteLine("<regist>wn</regist>");

                    SW.Flush();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        #endregion


        #region 로그인

        public String Login(String data)
        {
            String UserName = "*null*,*null*";

            try
            {
                // resultcode  // 0 : 아이디 잘못 입력 1 : 비번 잘못입력  2 : 로긴 성공
                int resultcode = 0;

                using (MySqlConnection conn = new MySqlConnection(DBInfo))
                {
                    // get Data
                    String[] co1 = { "<login " };
                    String[] d1 = data.Split(co1, StringSplitOptions.RemoveEmptyEntries);

                    String[] dd = d1[0].Split('>');

                    String[] co2 = { "</login" };
                    String[] ddd = dd[1].Split(co2, StringSplitOptions.RemoveEmptyEntries);

                    // DB Connection
                    conn.Open();
                    string sql = "SELECT* FROM user where id = '" + dd[0] + "'";

                    // Mysql Connection
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    MySqlDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        resultcode = 1;
                        if (rdr["password"].ToString().Equals(ddd[0]))
                        {
                            UserName = rdr["nickname"].ToString() + "," + dd[0];
                            resultcode = 2;
                            break;
                        }
                    }
                    rdr.Close();
                    conn.Close();
                }

                UserName += "," + resultcode;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }

            return UserName;
        }

        #endregion


        #region 유저 데이터 저장

        public void SaveData(String pkdata, String UserId)
        {
            pkdata = splitTag("userdata", pkdata);
            
            string[] co1 = {"|"};
            String[] data = pkdata.Split(co1, StringSplitOptions.None);
            
            if (data.Length == 29 || UserId.Equals(""))
                return;

            String query = "'" + UserId + "', '";
            String k_name = "";
            String u_query = "";

            
            try
            {
                for (int i = 1; i < data.Length; i++)
                {
                    if (data[i].Equals(""))
                    {
                        data[i] = "*null*";
                    }

                    query += data[i];

                    if (i != data.Length - 1)
                        query += "', '";
                    else
                        query += "'";
                }
                
                using (MySqlConnection conn = new MySqlConnection(DBInfo))
                {
                    // DB Connection
                    conn.Open();

                    string sql = "SELECT* FROM userinfo";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    MySqlDataReader rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        for (int i = 1; i < rdr.FieldCount; i++)
                        {
                            k_name += rdr.GetName(i);
                            u_query += rdr.GetName(i) + "='" + data[i] + "'";
                            if (i != rdr.FieldCount - 1)
                            {
                                k_name += ", ";
                                u_query += ", ";
                            }
                        }
                        break;
                    }

                    conn.Close();
                }

                using (MySqlConnection conn = new MySqlConnection(DBInfo))
                {
                    // DB Connection
                    conn.Open();
                    string sql = "";
                    if(u_query != "")
                        sql = "INSERT INTO userinfo VALUES(" + query + ") ON DUPLICATE KEY UPDATE " + u_query;
                    else
                        sql = "INSERT INTO userinfo VALUES(" + query + ") ON DUPLICATE KEY UPDATE id = ''";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }

            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }



        #endregion


        #region 유저 데이터 전송

        public void SendData(NetworkStream NS, String userid)
        {
            try
            {
                StreamWriter SW = new StreamWriter(NS, Encoding.UTF8);

                using (MySqlConnection conn = new MySqlConnection(DBInfo))
                {
                    // DB Connection
                    conn.Open();

                    string sql = "" +
                        "SELECT* FROM userinfo " +
                        "WHERE id = '" + userid + "'";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    MySqlDataReader rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        for (int i = 0; i < rdr.FieldCount; i++)
                          SW.WriteLine("<dataload>" + rdr[i].ToString() + "</dataload>");
                    }

                    SW.Flush();
                    rdr.Close();
                    conn.Close();
                }
            }
            catch (Exception e)
            {
               // MessageBox.Show(e.ToString());
            }
        }
        #endregion

        #region 몬스터 데이터 저장
        public void SaveMonster(String pkdata)
        {
            pkdata = splitTag("monster", pkdata);

            string[] co1 = { "," };
            String[] data = pkdata.Split(co1, StringSplitOptions.None);

            String query = "'";
            String u_query = "";
            try
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i].Equals(""))
                    {
                        data[i] = "nil";
                    }

                    query += data[i];

                    if (i != data.Length - 1)
                        query += "', '";
                    else
                        query += "'";
                }

                using (MySqlConnection conn = new MySqlConnection(DBInfo))
                {
                    // DB Connection
                    conn.Open();

                    string sql = "SELECT* FROM monster";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    MySqlDataReader rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        for (int i = 0; i < rdr.FieldCount; i++)
                        {
                            // 필드 이름 받아옴
                            u_query += rdr.GetName(i) + "='" + data[i] + "'";
                            if (i != rdr.FieldCount - 1)
                            {
                                u_query += ", ";
                            }
                        }
                        break;
                    }

                    conn.Close();
                }

                using (MySqlConnection conn = new MySqlConnection(DBInfo))
                {
                    // DB Connection
                    conn.Open();
                    string sql = "";
                    if (u_query != "") // DB에 해당 맵의 이벤트가 없다면 새로 추가
                    {
                        sql = "INSERT INTO monster VALUES(" + query + ") ON DUPLICATE KEY UPDATE " + u_query;
                    }
                    else
                    {
                        sql = "INSERT INTO monster VALUES(" + query + ")";
                    }
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }

            catch (Exception e)
            {
              // MessageBox.Show(e.Message);
            }
        }
        #endregion

        #region 몬스터 데이터 전송

        // 해당 맵의 몬스터 정보를 요청함 처음에만?
        public void SendMonster(NetworkStream NS, String pkdata)
        {
            StreamWriter SW = new StreamWriter(NS, Encoding.UTF8);
            pkdata = splitTag("req_monster", pkdata); // mapid를 추출함

            try
            {
                using (MySqlConnection conn = new MySqlConnection(DBInfo))
                {
                    // DB Connection
                    conn.Open();

                    string sql = "" +
                        "SELECT* FROM monster " +
                        "WHERE mapid = '" + pkdata + "'";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    MySqlDataReader rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        string data = "<23>";
                        for (int i = 0; i < rdr.FieldCount; i++)
                            data += rdr[i].ToString() + ",";
                        data += "</23>";
                        SW.WriteLine(data); // 해당 유저에게만 보냄 지금 여기서 문제;
                    }
                    SW.Flush();
                    rdr.Close();
                    conn.Close();
                }
            }
            catch (Exception e)
            {
                //MessageBox.Show(e.Message);
            }
        }
        #endregion

        #region 몬스터 젠 시간 1초마다 갱신
        public void respawnMonster()
        {
            using (MySqlConnection conn = new MySqlConnection(DBInfo))
            {
                // DB Connection
                conn.Open();
                try
                {
                    string sql = "" +
                        "UPDATE monster SET delay = delay - 10 " +
                        "WHERE hp = 0 AND delay > 0";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    MySqlDataReader rdr = cmd.ExecuteReader();
                    conn.Close();

                    conn.Open();
                    sql = "" +
                        "UPDATE monster SET delay = 0 " +
                        "WHERE delay < 0";
                    cmd = new MySqlCommand(sql, conn);
                    rdr = cmd.ExecuteReader();
                    conn.Close();
                }
                catch (Exception e)
                {
                    //MessageBox.Show(e.Message);
                    conn.Close();
                }
            }
        }
        #endregion

        #region 태그 나누기
        // Split Tag
        public String splitTag(String tag, String data)
        {
            string[] co1 = { "<" + tag + ">" };
            String[] d1 = data.Split(co1, StringSplitOptions.RemoveEmptyEntries);

            string[] co2 = { "</" + tag + ">" };
            String[] d2 = d1[0].Split(co2, StringSplitOptions.RemoveEmptyEntries);

            return d2[0];
        }
        #endregion
    }
}