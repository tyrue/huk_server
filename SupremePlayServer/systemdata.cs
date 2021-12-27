using System;
using System.Collections.Generic;
using System.Linq;

namespace SupremePlayServer
{
    public class Systemdata
    {
        public Dictionary<int, Dictionary<int, Monster>> monster_data; // 맵에 존재하는 몬스터의 데이터를 저장
        public Dictionary<int, List<Item2>> item_data2; // 맵에 존재하는 아이템의 데이터를 저장
        public Dictionary<int, string> map_data; // 맵의 이름 저장
        public System_DB system_db;
        public MainForm mainForm;
        Dictionary<int, int[]> party_quest_map_id;

        public List<string> random_server_msg;
        public Systemdata()
        {
            try
            {
                monster_data = new Dictionary<int, Dictionary<int, Monster>>();
                item_data2 = new Dictionary<int, List<Item2>>();

                map_data = new Dictionary<int, string>();
                system_db = new System_DB();
                map_data = system_db.SendMap();
                random_server_msg = new List<string>();
                make_random_msg(random_server_msg);

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

        public void make_random_msg(List<string> msg_list)
        {
            msg_list.Add("단축키 도움말은 F1키를 누르시면 볼 수 있습니다.");
            msg_list.Add("강력한 무기는 가끔씩 추가로 피해를 줄 수 있습니다.");
            msg_list.Add("부여성 남쪽의 해안가에서 파티퀘스트를 할 수 있습니다.");
            msg_list.Add("포목점에서 용의비늘을 감정할 수 있습니다.");
            msg_list.Add("놀이방의 피하기방에서 좋은 물건을 얻을 수 있을지도..");
            msg_list.Add("npc와의 대화는 마우스 클릭으로도 가능합니다.");
            msg_list.Add("죽었을 경우 마을의 성황당에서 부활할 수 있습니다.");
            msg_list.Add("강력한 적들은 원거리에서 공격할 수 도 있습니다.");
            msg_list.Add("\"/도움말\"을 입력하시면 명령어 도움말을 볼 수 있습니다.");
            msg_list.Add("주술사는 원거리에서 강력한 마법을 사용하는 직업입니다.");
            msg_list.Add("전사는 근거리에서 자신의 체력을 희생하여 강력한 한방을 주는 직업입니다.");
            msg_list.Add("도사는 파티원에게 이로운 마법을 걸어주거나 회복해줄 수 있는 직업입니다.");
            msg_list.Add("도적은 빠른 몸놀림으로 근거리에서 적을 상대하는 직업입니다.");
            msg_list.Add("레벨이 99가 되면 영혼사에서 경험치를 팔아서 체력, 마력을 살 수 있습니다.");
            msg_list.Add("푸줏간에선 고기를, 주막에선 술을 살 수 있습니다.");
            msg_list.Add("체력, 마력이 승급기준이 되면 부여 왕궁에서 승급 퀘스트를 진행 할 수 있습니다.");
            msg_list.Add("모든 직업의 승급은 4차까지 있습니다.");
            msg_list.Add("\"`\"키를 누르면 시스템 메뉴를 볼 수 있습니다.");
            msg_list.Add("부여성 북쪽으로 가면 세계전도로 갈 수 있습니다.");
            msg_list.Add("0~10, 20~30, 40~50분에는 선착장에서 일본으로 갈 수 있습니다.");
            msg_list.Add("10~20, 30~40, 50~60분에는 선착장에서 고균도로 갈 수 있습니다.");
            msg_list.Add("용무기는 극지방 대장간에서 은나무가지로 강화 할 수 있습니다.");
            msg_list.Add("일본, 중국 대장간에서 전설무기를 제작할 수 있습니다.");
            msg_list.Add("강력한 적들은 스스로 회복을 할 수 있지만 제한이 있습니다.");
            msg_list.Add("대화중 tap키를 눌러서 대화 타입을 변경할 수 있습니다.");
            msg_list.Add("아이템과 스킬은 마우스 오른쪽 버튼을 클릭해서 단축키 등록을 할 수 있습니다.");
            msg_list.Add("네이버 카페에서 다양한 공략과 소식을 볼 수 있습니다!");
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
            plist.Add("<monster_chat>"); // 몬스터 말풍선 표시
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
                int map_id = int.Parse(d[0]);
                int id = int.Parse(d[1]);

                if (!monster_data.ContainsKey(map_id))
                {
                    monster_data[map_id] = new Dictionary<int, Monster>();
                }

                if (monster_data[map_id].ContainsKey(id))
                {
                    var temp = monster_data[map_id][id];
                    temp.hp = int.Parse(d[2]);
                    temp.x = int.Parse(d[3]);
                    temp.y = int.Parse(d[4]);
                    temp.direction = int.Parse(d[5]);
                    temp.respawn = int.Parse(d[6]);
                    temp.dead = temp.hp <= 0 ? true : false;
                }
                else
                {
                    Monster m = new Monster();
                    m.map_id = map_id;
                    m.id = id;
                    m.hp = int.Parse(d[2]);
                    m.x = int.Parse(d[3]);
                    m.y = int.Parse(d[4]);
                    m.direction = int.Parse(d[5]);
                    m.respawn = int.Parse(d[6]);
                    m.dead = m.hp <= 0 ? true : false;
                    monster_data[map_id][id] = m;
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
                var d = from i in monster_data.Values
                        from ii in i.Values
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
