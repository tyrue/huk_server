using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupremePlayServer
{
    public class Player
    {
        public int map_id;
        public int id;
        public int x;
        public int y;
        public long hp;
        public int sp;
        public int direction;

        public bool dead;
        public Dictionary<int, int> buffTime;
        public bool stealth; // 잠행인가


        public Player()
        {
            map_id = 0;
            id = 0;
            x = 0;
            y = 0;
            hp = 0;
            sp = 0;
            direction = 0;
            dead = false;
            buffTime = new Dictionary<int, int>();
            stealth = false;
        }

        public bool isAggroFree()
        {
            if (stealth) return true;
            return false;
        }
    }
}
