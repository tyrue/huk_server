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
        public Dictionary<int, int[]> party_quest_map_id;

        public Dictionary<string, int> packetMessageDict;
        public Dictionary<string, int> mapPacketMessageDict;
        public Dictionary<string, int> ignoreMessageDict;

        public List<string> random_server_msg;

        public System_DB system_db;
        public MainForm mainForm;
        public Systemdata()
        {
            try
            {
                var comparer = StringComparer.OrdinalIgnoreCase;
                monster_data = new Dictionary<int, Dictionary<int, Monster>>();
                item_data2 = new Dictionary<int, List<Item2>>();
                map_data = new Dictionary<int, string>();

                packetMessageDict = new Dictionary<string, int>(comparer);
                mapPacketMessageDict= new Dictionary<string, int>(comparer);
                ignoreMessageDict = new Dictionary<string, int>(comparer);

                system_db = new System_DB();
                map_data = system_db.SendMap();
                random_server_msg = new List<string>();

                makePacketMessageDict(packetMessageDict);
                makeMapPacketMessageDict(mapPacketMessageDict);
                makeignoreMessageDict(ignoreMessageDict);
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

        public void makePacketMessageDict(Dictionary<string, int> dict)
        {
            // 메시지 관련
            dict.Add("<chat>", 0);    // 공지
            dict.Add("<chat1>", 0);   // 일반 채팅
            dict.Add("<bigsay>", 0);  // 외치기
            dict.Add("<whispers>", 0); // 귓속말
            dict.Add("<System_Message>", 0);

            // 길드 관련
            dict.Add("<Guild_Message>", 0); // 길드 메시지
            dict.Add("<guild_group>", 0);
            dict.Add("<guild_invite>", 0);
            dict.Add("<guild_delete>", 0);
            
            // 운영자 권한 관련
            dict.Add("<summon>", 0);
            dict.Add("<all_summon>", 0);
            dict.Add("<prison>", 0);  // 감옥
            dict.Add("<cashgive>", 0);

            // 파티 관련
            dict.Add("<party>", 0);   // 파티
            dict.Add("<partymessage>", 0);
            dict.Add("<party_no>", 0);
            dict.Add("<nptreq>", 0);
            dict.Add("<nptno>", 0);
            dict.Add("<nptyes>", 0);
            dict.Add("<nptout>", 0);

            // 교환 관련
            dict.Add("<trade_invite>", 0);
            dict.Add("<trade_system>", 0);
            dict.Add("<trade_item>", 0);
            dict.Add("<trade_money>", 0);
            dict.Add("<trade_okay>", 0);
            dict.Add("<trade_fail>", 0);

            // 기타
            dict.Add("<switches>", 0); // 스위치 공유
            dict.Add("<variables>", 0); // 변수 공유
            dict.Add("<8>", 0); // 유저 죽음 알림
        }

        public void makeMapPacketMessageDict(Dictionary<string, int> dict)
        {
            // 몬스터 관련
            dict.Add("<aggro>", 0); // 몬스터 어그로
            dict.Add("<respawn>", 0); // 몹 부활 공유
            dict.Add("<hp>", 0); // 몹 체력 공유
            dict.Add("<enemy_dead>", 0); // 몹 죽음 공유
            dict.Add("<mon_move>", 0); // 몬스터 이동 공유
            dict.Add("<mon_damage>", 0); // 몬스터 데미지 표시
            dict.Add("<monster>", 0); // 몬스터 정보 공유

            // 애니메이션 관련
            dict.Add("<event_animation>", 0);
            dict.Add("<player_animation>", 0);
            dict.Add("<27>", 0); // 애니메이션 재생

            // 파티 관련
            dict.Add("<partyhill>", 0); // 파티 힐
            dict.Add("<nptgain>", 0); // 파티 아이템 획득
            dict.Add("<npt_move>", 0); // 파티 맵 이동

            // 플레이어 관련
            dict.Add("<player_damage>", 0); // 플레이어 데미지 표시
            dict.Add("<map_chat>", 0); // 플레이어 말풍선 표시

            // 아이템 드랍 관련
            dict.Add("<Drop>", 0);    // 템 드랍
            dict.Add("<Drop_Get>", 0);    // 템 삭제
            dict.Add("<drop_create>", 0); // 템 드랍
            dict.Add("<drop_del>", 0);    // 템 삭제
            
            // pvp 관련
            dict.Add("<attack_effect>", 0); // pvp 평타
            dict.Add("<skill_effect>", 0); // pvp 스킬

            // 기타
            dict.Add("<se_play>", 0); // 효과음 실행
            dict.Add("<monster_chat>", 0); // 몬스터 말풍선 표시
            dict.Add("<show_range_skill>", 0); // 스킬 
        }

        public void makeignoreMessageDict(Dictionary<string, int> dict)
        {
            dict.Add("<mon_move>", 0);
            dict.Add("<aggro>", 0);
            dict.Add("<mon_damage>", 0);
            dict.Add("<player_damage>", 0);
            dict.Add("<enemy_dead>", 0);
            dict.Add("<monster>", 0);
            dict.Add("<hp>", 0);
            dict.Add("<drop_del>", 0);
            dict.Add("<del_item>", 0);
            dict.Add("<drop_create>", 0);
            dict.Add("<userdata>", 0);
            dict.Add("<Drop>", 0);
            dict.Add("<show_range_skill>", 0);
            dict.Add("<monster_sp>", 0);
            dict.Add("<Drop_Get>", 0);
            dict.Add("<27>", 0);
            dict.Add("<nptgain>", 0);
            dict.Add("<partyhill>", 0);
            dict.Add("<npt_move>", 0);
            dict.Add("<attack_effect>", 0);
            dict.Add("<skill_effect>", 0);
            dict.Add("<monster_chat>", 0);
        }

        public void SaveMonster(string data)
        {
            try
            {
                string[] d = data.Split(',');
                int map_id = int.Parse(d[0]);
                int id = int.Parse(d[1]);
                int hp, x, y, direction, respawn;
                respawn = int.TryParse(d[6], out respawn) ? respawn : -1;
                
                if (!monster_data.ContainsKey(map_id))
                {
                    monster_data[map_id] = new Dictionary<int, Monster>();
                }

                if(!monster_data[map_id].ContainsKey(id))
                {
                    Monster m = new Monster();
                    m.map_id = map_id;
                    m.id = id;
                    monster_data[map_id][id] = m;
                }

                var temp = monster_data[map_id][id];
                if (int.TryParse(d[2], out hp)) temp.hp = hp;
                if (int.TryParse(d[3], out x)) temp.x = x;
                if (int.TryParse(d[4], out y)) temp.y = y;
                if (int.TryParse(d[5], out direction)) temp.direction = direction;
                if (int.TryParse(d[6], out respawn)) temp.respawn = respawn;
                temp.dead = temp.hp <= 0 ? true : false;
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
            }
        }

        public void SaveMonster2(string data)
        {
            try
            {
                string[] d = data.Split(',');
                int map_id = int.Parse(d[0]);
                int id = int.Parse(d[1]);
                int mon_id = int.Parse(d[2]);
                int x = 0;
                int y = 0;
                bool sw = false;

                if(d.Length > 3)
                {
                    x = int.Parse(d[3]);
                    y = int.Parse(d[4]);
                    if (int.Parse(d[5]) == -1) sw = true;
                }

                if (!monster_data.ContainsKey(map_id))
                {
                    monster_data[map_id] = new Dictionary<int, Monster>();
                }

                if (!monster_data[map_id].ContainsKey(id))
                {
                    Monster m = new Monster();
                    m.map_id = map_id;
                    m.id = id;
                    monster_data[map_id][id] = m;
                }

                var temp = monster_data[map_id][id];
                temp.mon_id = mon_id;
                if (x != 0) temp.x = x;
                if (y != 0) temp.y = y;
                temp.delete_sw = sw;
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
            }
        }

        public void DeleteMonster(string data)
        {
            try
            {
                // id, 이벤트 id, map id, netparty장 이름
                string[] d = data.Split(',');
                int id = int.Parse(d[0]);
                int map_id = int.Parse(d[1]);

                if (!monster_data.ContainsKey(map_id)) return;
                if (!monster_data[map_id].ContainsKey(id)) return;
                if (monster_data[map_id][id].delete_sw) monster_data[map_id].Remove(id);

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
            int[] data = new int[2]; // sw_id, on/off
            try
            {
                if (party_quest_map_id.ContainsKey(id))
                {
                    int map_id = party_quest_map_id[id][0];
                    data[0] = party_quest_map_id[id][1]; // 스위치 id

                    if (!mainForm.MapUser2.ContainsKey(map_id)) data[1] = 0;
                    else if (mainForm.MapUser2[map_id].Count <= 0) data[1] = 0;
                    else data[1] = 1; // 만약 해당 맵에 사람이 있다면 파티 퀘스트 체크 스위치 
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
