using System;
using System.Collections.Generic;
using System.Linq;

namespace SupremePlayServer
{
    public class Systemdata
    {
        public Dictionary<int, List<Monster>> monster_data; // 맵에 존재하는 몬스터의 데이터를 저장
        public Dictionary<int, List<Item2>> item_data2; // 맵에 존재하는 아이템의 데이터를 저장
        public Dictionary<int, string> map_data; // 맵의 이름 저장
        public System_DB system_db;
        public MainForm mainForm;
        Dictionary<int, int[]> party_quest_map_id;
        public Systemdata()
        {
            try
            {
                monster_data = new Dictionary<int, List<Monster>>();
                item_data2 = new Dictionary<int, List<Item2>>();

                map_data = new Dictionary<int, string>();
                system_db = new System_DB();
                map_data = system_db.SendMap();
                
                // 파티 퀘스트 맵 아이디 저장
                party_quest_map_id = new Dictionary<int, int[]>();
                party_quest_map_id.Add(1, new int[] { 51,   1015 });
                party_quest_map_id.Add(2, new int[] { 113,  1143 });
                party_quest_map_id.Add(3, new int[] { 404,  1152 });
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
            }
        }

        public List<String> getAllpacketList()
        {
            List<String> plist = new List<string>();

            plist.Add("<chat>");    // 공지
            plist.Add("<chat1>");   // 일반 채팅
            plist.Add("<bigsay>");  // 외치기
            plist.Add("<23>");      // 몹 정보 공유
            plist.Add("<27>");      // 애니메이션 공유
            plist.Add("<partyhill>"); // 파티 힐
            plist.Add("<Drop>");    // 템 드랍
            plist.Add("<Drop_Get>");    // 템 삭제
            plist.Add("<drop_create>"); // 템 드랍
            plist.Add("<drop_del>");    // 템 삭제
            plist.Add("<Guild_Message>"); // 길드 메시지
            plist.Add("<party>");   // 파티
            plist.Add("<summon>");
            plist.Add("<all_summon>");
            plist.Add("<prison>");  // 감옥
            plist.Add("<partymessage>");
            plist.Add("<whispers>");
            plist.Add("<party_no>");
            plist.Add("<System_Message>");
            plist.Add("<event_animation>");
            plist.Add("<guild_group>");
            plist.Add("<guild_invite>");
            plist.Add("<guild_delete>");
            plist.Add("<guild_message>");
            plist.Add("<player_animation>");
            plist.Add("<trade_invite>");
            plist.Add("<trade_system>");
            plist.Add("<trade_item>");
            plist.Add("<trade_money>");
            plist.Add("<trade_okay>");
            plist.Add("<trade_fail>");
            plist.Add("<nptreq>");
            plist.Add("<nptno>");
            plist.Add("<nptyes>");
            plist.Add("<nptout>");
            plist.Add("<nptgain>");
            plist.Add("<npt_move>"); // 파티 장소 이동
            plist.Add("<cashgive>");
            plist.Add("<switches>"); // 스위치 공유
            plist.Add("<variables>"); // 변수 공유
            plist.Add("<enemy_dead>"); // 몹 죽음 공유
            plist.Add("<respawn>"); // 몹 부활 공유
            plist.Add("<8>"); // 유저 죽음 알림
            plist.Add("<hp>"); // 몹 체력 공유
            plist.Add("<mon_move>"); // 몬스터 이동 공유
            plist.Add("<aggro>"); // 몬스터 어그로
            plist.Add("<mon_damage>"); // 몬스터 데미지 표시
            plist.Add("<player_damage>"); // 플레이어 데미지 표시
            plist.Add("<map_chat>"); // 플레이어 말풍선 표시
            plist.Add("<attack_effect>"); // pvp 평타
            plist.Add("<skill_effect>"); // pvp 스킬
            plist.Add("<show_range_skill>"); // 스킬 보여주기
            return plist;
        }

        public void SaveMonster(string data)
        {
            try
            {
                string[] d = data.Split(',');
                bool sw = false;
                int id = int.Parse(d[0]);
                if (!monster_data.ContainsKey(id))
                {
                    monster_data[id] = new List<Monster>();
                    sw = false;
                }
                else
                {
                    Monster ii = null;

                    var temp = from i in monster_data[id]
                               where (i.id == int.Parse(d[1]))
                               select i;
                    if (temp != null && temp.Count() > 0)
                    {
                        sw = true;
                        ii = temp.First();
                        ii.hp = int.Parse(d[2]);
                        ii.x = int.Parse(d[3]);
                        ii.y = int.Parse(d[4]);
                        ii.direction = int.Parse(d[5]);
                        ii.respawn = int.Parse(d[6]);
                        ii.dead = ii.hp <= 0 ? true : false;
                    }
                    else
                    {
                        sw = false;
                    }
                }

                if (!sw)
                {
                    Monster m = new Monster();
                    m.map_id = id;
                    m.id = int.Parse(d[1]);
                    m.hp = int.Parse(d[2]);
                    m.x = int.Parse(d[3]);
                    m.y = int.Parse(d[4]);
                    m.direction = int.Parse(d[5]);
                    m.respawn = int.Parse(d[6]);
                    m.dead = m.hp <= 0 ? true : false;
                    monster_data[m.map_id].Add(m);
                }
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
            }
        }

        public void SaveItem2(string data)
        {
            try
            {
                string[] d = data.Split(',');
                Item2 i = new Item2();

                i.d_id = int.Parse(d[0]);
                i.type2 = int.Parse(d[1]);
                i.type1 = int.Parse(d[2]);
                i.id = int.Parse(d[3]);
                i.map_id = int.Parse(d[4]);
                i.x = int.Parse(d[5]);
                i.y = int.Parse(d[6]);
                i.num = int.Parse(d[7]);

                if (!item_data2.ContainsKey(i.map_id))
                    item_data2[i.map_id] = new List<Item2>();
                item_data2[i.map_id].Add(i);
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
            }
        }


        public void DelItem2(string data) // 맵id, id
        {
            try
            {
                string[] s = data.Split(',');
                int map_id = int.Parse(s[1]);
                int id = int.Parse(s[0]);

                if (!item_data2.ContainsKey(map_id)) return;
                var d = from i in item_data2[map_id]
                        where i.d_id == id
                        select i;
                if (d != null && d.Count() > 0)
                    item_data2[map_id].Remove(d.First());
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
            }
        }

        public string SendMap(int id)
        {
            try
            {
                if (map_data.ContainsKey(id)) return map_data[id];
                else
                {
                    map_data = system_db.SendMap();
                    if (map_data.ContainsKey(id)) return map_data[id];
                    else return "null";
                }
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
                return "null";
            }
        }

        public List<string> respawnMonster2()
        {
            try
            {
                List<string> list = new List<string>();
                var d = from i in monster_data
                        from ii in i.Value
                        where (ii.respawn > 0 && ii.dead)
                        select ii;

                foreach (var data in d)
                {
                    data.respawn -= 60;
                    if (data.respawn <= 0)
                    {
                        data.respawn = 0;
                        string s = data.map_id + "," + data.id + "," + data.x + "," + data.y + "," + data.direction;
                        data.dead = false;
                        list.Add(s);
                    }
                }
                return list;
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
                return null;
            }
        }

        public void DelAllItem()
        {
            item_data2.Clear();
        }

        public int[] checkPartyQuest(int id)
        {
            int[] data = new int[2]; // id, on/off
            try
            {
                if (party_quest_map_id.ContainsKey(id))
                {
                    int map_id = party_quest_map_id[id][0];
                    data[0] = party_quest_map_id[id][1]; // 스위치 id
                    if (mainForm.MapUser2.ContainsKey(map_id)) // 만약 해당 맵에 사람이 있다면 파티 퀘스트 체크 스위치 on
                    {
                        if (mainForm.MapUser2[map_id].Count > 0)
                        {
                            data[1] = 1;
                        }
                        else
                        {
                            data[1] = 0;
                        }
                    }
                    else
                    {
                        data[1] = 0;
                    }
                }
            }
            catch(Exception e)
            {
                mainForm.write_log(e.ToString());
            }
            return data;
        }
    }
}
