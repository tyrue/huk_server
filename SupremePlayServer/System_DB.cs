using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        public System_DB(mainForm mainForm) : base(mainForm)
        {
            this.mainForm = mainForm;
        }

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(DBInfo);
        }

        private async Task<bool> CheckIfExistsAsync(MySqlConnection conn, string query, Dictionary<string, object> parameters)
        {
            using (var cmd = new MySqlCommand(query, conn))
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value);
                }

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    return await reader.ReadAsync();
                }
            }
        }

        #region 회원가입
        public async Task RegisterationAsync(UserThread user, string tag, string body)
        {
            try
            {
                int resultcode = 0;
                string[] co = { "," };
                var data = body.Split(co, StringSplitOptions.RemoveEmptyEntries);

                using (var conn = GetConnection())
                {
                    await conn.OpenAsync();

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
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
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
                        await conn.OpenAsync();

                        var insertQuery = "INSERT INTO user VALUES (@nickname, @id, @password, NOW())";
                        using (var cmd = new MySqlCommand(insertQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@nickname", data[0]);
                            cmd.Parameters.AddWithValue("@id", data[1]);
                            cmd.Parameters.AddWithValue("@password", data[2]);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        await user.SendMessageWithTagAsync(tag, "success");
                    }
                }
                else if (resultcode == 1)
                {
                    await user.SendMessageWithTagAsync(tag, "wi"); // 이미 존재하는 아이디
                }
                else if (resultcode == 2)
                {
                    await user.SendMessageWithTagAsync(tag, "wn"); // 이미 존재하는 닉네임
                }
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
            }
        }
        #endregion

        #region 로그인
        public async Task<string> LoginAsync(string message)
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
                    await conn.OpenAsync();
                    var query = "SELECT nickname, password FROM user WHERE id = @id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
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

        public async Task SaveLoginDate(string userId)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync();
                    var checkQuery = "SELECT 1 FROM userinfo WHERE id = @id";
                    var exists = await CheckIfExistsAsync(conn, checkQuery, new Dictionary<string, object> { { "@id", userId } });

                    if (exists)
                    {
                        var updateQuery = "UPDATE user SET last_login_date = @date WHERE id = @id";
                        using (var cmd = new MySqlCommand(updateQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", userId);
                            cmd.Parameters.AddWithValue("@date", DateTime.Now);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }   
                }
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
            }
        }

        #region 유저 데이터 저장
        public async Task SaveData2Async(string pkdata, UserThread user)
        {
            try
            {
                var userId = user.userId;
                var userName = user.userName;
                var dataDict = ParseKeyValueData(pkdata);

                if (string.IsNullOrEmpty(userId)) return;
                if (dataDict.ContainsKey("nickname") && !dataDict["nickname"].Equals(userName))
                {
                    user.userName = dataDict["nickname"];
                    
                    mainForm.UserByNameDict.TryRemove(userName, out user);
                    mainForm.UserByNameDict.TryAdd(user.userName, user);
                    userName = user.userName;
                }

                using (var conn = GetConnection())
                {
                    await conn.OpenAsync();

                    // Check if the record exists
                    var checkQuery = "SELECT 1 FROM userinfo WHERE id = @id";
                    var exists = await CheckIfExistsAsync(conn, checkQuery, new Dictionary<string, object> { { "@id", userId } });

                    if (exists)
                    {
                        string updateQuery = "UPDATE userinfo SET ";
                        List<string> updateValues = new List<string>();

                        foreach (var pair in dataDict)
                        {
                            updateValues.Add($"{pair.Key} = '{pair.Value}'");
                        }

                        updateQuery += string.Join(", ", updateValues);
                        updateQuery += $" WHERE id = '{userId}'";

                        MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn);
                        await updateCmd.ExecuteNonQueryAsync();
                        
                        updateQuery = "UPDATE user SET ";
                        updateQuery += $"nickname = '{userName}'";
                        updateQuery += $"WHERE id = '{userId}'";

                        updateCmd = new MySqlCommand(updateQuery, conn);
                        await updateCmd.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        // Insert new record
                        dataDict.Add("id", userId);
                        string insertQuery = $"INSERT INTO userinfo ({string.Join(", ", dataDict.Keys)}) " +
                                             $"VALUES ('{string.Join("', '", dataDict.Values)}')";
                        MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn);
                        await insertCmd.ExecuteNonQueryAsync();
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
        public async Task SendDataAsync(UserThread user, string userid)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync(new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token);

                    var query = "SELECT * FROM userinfo WHERE id = @id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", userid);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    await user.SendMessageWithTagAsync("dataload", $"{reader.GetName(i)}:{reader[i]}");
                                }
                                await user.SendMessageWithTagAsync("dataLoadEnd", "ok");
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
                    // 동기적으로 연결을 연다.
                    conn.Open();

                    var query = "SELECT * FROM map_name ORDER BY id ASC";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        // 동기적으로 ExecuteReader 호출
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
        public async Task SaveMapAsync(string pkdata)
        {
            pkdata = splitTag("map_name", pkdata);
            string[] co = { "," };
            var data = pkdata.Split(co, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync();

                    var checkQuery = "SELECT 1 FROM map_name WHERE id = @id";
                    var exists = await CheckIfExistsAsync(conn, checkQuery, new Dictionary<string, object> { { "@id", data[0] } });

                    if (exists)
                    {
                        var updateQuery = "UPDATE map_name SET name = @name WHERE id = @id";
                        using (var cmd = new MySqlCommand(updateQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", data[0]);
                            cmd.Parameters.AddWithValue("@name", data[1]);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        var insertQuery = "INSERT INTO map_name (id, name) VALUES (@id, @name)";
                        using (var cmd = new MySqlCommand(insertQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", data[0]);
                            cmd.Parameters.AddWithValue("@name", data[1]);
                            await cmd.ExecuteNonQueryAsync();
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

        #region 유저 닉네임에 따른 Ban 데이터 저장
        public async Task SaveUserBanByNickname(UserThread user, DateTime dateTime, string reason)
        {

            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync();

                    var checkQuery = "SELECT 1 FROM ban_user_list WHERE nickname = @nickname";
                    var exists = await CheckIfExistsAsync(conn, checkQuery, new Dictionary<string, object> { { "@nickname", user.userName } });

                    if (exists)
                    {
                        var updateQuery = "UPDATE ban_user_list SET end_date = @end_date, reason = @reason  WHERE nickname = @nickname";
                        using (var cmd = new MySqlCommand(updateQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@end_date", dateTime);
                            
                            cmd.Parameters.AddWithValue("@reason", reason);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        var insertQuery = "INSERT INTO ban_user_list (nickname, end_date, reason) VALUES (@nickname, @end_date, @reason)";
                        using (var cmd = new MySqlCommand(insertQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@nickname", dateTime);
                            cmd.Parameters.AddWithValue("@end_date", dateTime);

                            cmd.Parameters.AddWithValue("@reason", reason);
                            await cmd.ExecuteNonQueryAsync();
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
