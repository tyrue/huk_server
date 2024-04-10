using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace SupremePlayServer
{
    public class System_DB : UserThread
    {
        private string DBInfo =

            "Server=127.0.0.1;" +
            // aws 외부 접속 db 주소
            //"Server=database-1.c3c2a4qqcid0.ap-northeast-2.rds.amazonaws.com;" +

            "Uid=root;" +
            "Pwd=abs753951;" +
            "Database=supremeplay;" +
            "CharSet=utf8;";


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
                mainform.write_log(e.ToString());
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
                mainform.write_log(e.ToString());
            }

            return UserName;
        }

        #endregion


        #region 유저 데이터 저장
        public void SaveData2(string pkdata, string userId)
        {
            if (string.IsNullOrEmpty(userId)) return;

            try
            {
                pkdata = splitTag("userdata", pkdata); // "userdata" 태그 분리

                Dictionary<string, string> dataDict = ParseKeyValueData(pkdata); // 키:값 형식 데이터 파싱

                if (!dataDict.ContainsKey("nickname") || string.IsNullOrEmpty(dataDict["nickname"]))
                    return;

                using (MySqlConnection conn = new MySqlConnection(DBInfo))
                {
                    conn.Open();

                    // 중복된 닉네임이 있는지 확인
                    string selectQuery = $"SELECT * FROM userinfo WHERE nickname = '{dataDict["nickname"]}'";
                    MySqlCommand selectCmd = new MySqlCommand(selectQuery, conn);
                    MySqlDataReader reader = selectCmd.ExecuteReader();

                    bool existingRecord = reader.Read();
                    reader.Close();

                    if (existingRecord) // 이미 존재하는 레코드가 있을 경우
                    {
                        string updateQuery = "UPDATE userinfo SET ";
                        List<string> updateValues = new List<string>();

                        
                        foreach (var pair in dataDict)
                        {
                            // 닉네임을 제외한 키:값들로 업데이트 쿼리 생성
                            if (pair.Key != "nickname")
                            {
                                updateValues.Add($"{pair.Key} = '{pair.Value}'");
                            }
                        }

                        updateQuery += string.Join(", ", updateValues);
                        updateQuery += $" WHERE nickname = '{dataDict["nickname"]}'";

                        MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn);
                        updateCmd.ExecuteNonQuery();
                    }
                    else // 존재하지 않는 경우, 새로운 레코드 삽입
                    {
                        string insertQuery = $"INSERT INTO userinfo ({string.Join(", ", dataDict.Keys)}) " +
                                             $"VALUES ('{string.Join("', '", dataDict.Values)}')";
                        MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn);
                        insertCmd.ExecuteNonQuery();
                    }

                    conn.Close();
                }
            }
            catch (Exception e)
            {
                mainform.write_log(e.ToString()); // 예외 발생 시 로그 출력
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
                        {
                            SW.WriteLine("<dataload>" + rdr.GetName(i) + ":" + rdr[i].ToString() + "</dataload>");
                        }
                        SW.WriteLine("<dataLoadEnd>ok</dataLoadEnd>");
                    }

                    SW.Flush();
                    rdr.Close();
                    conn.Close();
                }
            }
            catch (Exception e)
            {
                mainform.write_log(e.ToString());
            }
        }
        #endregion

        public Dictionary<int, string> SendMap()
        {
            try
            {
                Dictionary<int, string> data = new Dictionary<int, string>();
                using (MySqlConnection conn = new MySqlConnection(DBInfo))
                {
                    
                    // DB Connection
                    conn.Open();
                    

                    string sql = "" +
                        "SELECT* FROM map_name " +
                        "ORDER BY id ASC";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    MySqlDataReader rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        data[int.Parse(rdr[0].ToString())] = rdr[1].ToString();
                    }
                    conn.Close();
                }
                return data;
            }
            catch (Exception e)
            {
                if(mainform == null)
                {
                    MessageBox.Show(e.ToString());
                }
                mainform.write_log(e.ToString());
                return null;
            }
        }

        #region 맵 id에 대한 이름 저장
        public void SaveMap(String pkdata)
        {
            pkdata = splitTag("map_name", pkdata);

            string[] co1 = { "," };
            String[] data = pkdata.Split(co1, StringSplitOptions.RemoveEmptyEntries);

            String query = "\"";
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
                        query += "\", \"";
                    else
                        query += "\"";
                }

                using (MySqlConnection conn = new MySqlConnection(DBInfo))
                {
                    // DB Connection
                    conn.Open();

                    string sql = "" +
                        "SELECT* FROM map_name" +
                        " WHERE (id = " + data[0] + ")";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    MySqlDataReader rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        for (int i = 0; i < rdr.FieldCount; i++)
                        {
                            // 필드 이름 받아옴
                            u_query += rdr.GetName(i) + "=\"" + data[i] + "\"";
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
                        sql = "" +
                            "UPDATE map_name" +
                            " SET " + u_query +
                            " WHERE (id = " + data[0] + ")";
                    }
                    else
                    {
                        sql = "INSERT INTO map_name VALUES(" + query + ")";
                    }
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }

            catch (Exception e)
            {
                mainform.write_log(e.ToString());
            }
        }
        #endregion
    }
}