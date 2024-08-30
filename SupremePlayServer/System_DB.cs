using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SupremePlayServer
{
    public class System_DB : UserThread
    {
        public string DBInfo =
        //"Server=127.0.0.1;" +
        //"Server=database-1.c3c2a4qqcid0.ap-northeast-2.rds.amazonaws.com;" +

        "Uid=root;" +
        "Pwd=abs753951;" +
        "Database=supremeplay;" +
        "CharSet=utf8;";

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(DBInfo);
        }

        private bool CheckIfExists(MySqlConnection conn, string query, Dictionary<string, object> parameters)
        {
            using (var cmd = new MySqlCommand(query, conn))
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value);
                }

                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read();
                }
            }
        }

        #region 회원가입
        public void Registeration(UserThread user, string tag, string body)
        {
            try
            {
                int resultcode = 0;
                string[] co = { "," };
                var data = body.Split(co, StringSplitOptions.RemoveEmptyEntries);

                using (var conn = GetConnection())
                {
                    conn.Open();

                    // Check if ID or nickname already exists
                    var checkQuery = "SELECT id, nickname FROM user WHERE id = @id OR nickname = @nickname";
                    var parameters = new Dictionary<string, object>
                    {
                        {"@id", data[1]},
                        {"@nickname", data[0]}
                    };

                    using (var cmd = new MySqlCommand(checkQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", data[1]);
                        cmd.Parameters.AddWithValue("@nickname", data[0]);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (reader["id"].ToString() == data[1])
                                {
                                    resultcode = 1; // 아이디 이미 있음
                                    break;
                                }
                                if (reader["nickname"].ToString() == data[0])
                                {
                                    resultcode = 2; // 닉네임 이미 있음
                                    break;
                                }
                            }
                        }
                    }
                }

                if (resultcode == 0)
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();

                        var insertQuery = "INSERT INTO user VALUES (@nickname, @id, @password, NOW())";
                        using (var cmd = new MySqlCommand(insertQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@nickname", data[0]);
                            cmd.Parameters.AddWithValue("@id", data[1]);
                            cmd.Parameters.AddWithValue("@password", data[2]);
                            cmd.ExecuteNonQuery();
                        }

                        user.SendMessageWithTag(tag, "success");
                    }
                }
                else if (resultcode == 1)
                {
                    user.SendMessageWithTag(tag, "wi"); // 이미 존재하는 아이디
                }
                else if (resultcode == 2)
                {
                    user.SendMessageWithTag(tag, "wn"); // 이미 존재하는 닉네임
                }
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
            }
        }
        #endregion

        #region 로그인
        public string Login(string message)
        {
            string userName = "*null*,*null*";
            try
            {
                var data = message.Split('|');
                var id = data[0];
                var password = data[1];
                int resultcode = 0;

                using (var conn = GetConnection())
                {
                    conn.Open();

                    var query = "SELECT nickname, password FROM user WHERE id = @id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                resultcode = 1; // 아이디는 있음
                                if (reader["password"].ToString() == password)
                                {
                                    userName = $"{reader["nickname"]},{id}";
                                    resultcode = 2; // 로그인 성공
                                }
                            }
                        }
                    }
                }

                userName += $",{resultcode}";
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
            }

            return userName;
        }
        #endregion

        #region 유저 데이터 저장
        public void SaveData2(string pkdata, string userId)
        {
            if (string.IsNullOrEmpty(userId)) return;

            try
            {
                var dataDict = ParseKeyValueData(pkdata);
                if (!dataDict.ContainsKey("nickname") || string.IsNullOrEmpty(dataDict["nickname"]))
                    return;

                using (var conn = GetConnection())
                {
                    conn.Open();

                    // Check if the record exists
                    var checkQuery = "SELECT 1 FROM userinfo WHERE nickname = @nickname";
                    var exists = CheckIfExists(conn, checkQuery, new Dictionary<string, object> { { "@nickname", dataDict["nickname"] } });

                    if (exists)
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
                    else
                    {
                        // Insert new record
                        dataDict.Add("id", userId);
                        string insertQuery = $"INSERT INTO userinfo ({string.Join(", ", dataDict.Keys)}) " +
                                             $"VALUES ('{string.Join("', '", dataDict.Values)}')";
                        MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn);
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
            }
        }
        #endregion

        #region 유저 데이터 전송
        public void SendData(UserThread user, string userid)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    var query = "SELECT * FROM userinfo WHERE id = @id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", userid);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    user.SendMessageWithTag("dataload", $"{reader.GetName(i)}:{reader[i]}");
                                }
                                user.SendMessageWithTag("dataLoadEnd", "ok");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
            }
        }
        #endregion

        #region 맵 이름 전송
        public Dictionary<int, string> SendMap()
        {
            var data = new Dictionary<int, string>();
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    var query = "SELECT * FROM map_name ORDER BY id ASC";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                data[int.Parse(reader["id"].ToString())] = reader["name"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                mainForm?.write_log(e.ToString());
            }
            return data;
        }
        #endregion

        #region 맵 id에 대한 이름 저장
        public void SaveMap(string pkdata)
        {
            pkdata = splitTag("map_name", pkdata);
            string[] co = { "," };
            var data = pkdata.Split(co, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    var checkQuery = "SELECT 1 FROM map_name WHERE id = @id";
                    var exists = CheckIfExists(conn, checkQuery, new Dictionary<string, object> { { "@id", data[0] } });

                    if (exists)
                    {
                        var updateQuery = "UPDATE map_name SET name = @name WHERE id = @id";
                        using (var cmd = new MySqlCommand(updateQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", data[0]);
                            cmd.Parameters.AddWithValue("@name", data[1]);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        var insertQuery = "INSERT INTO map_name (id, name) VALUES (@id, @name)";
                        using (var cmd = new MySqlCommand(insertQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", data[0]);
                            cmd.Parameters.AddWithValue("@name", data[1]);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
            }
        }
        #endregion
    }
}
