using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupremePlayServer
{
    public class Monster
    {
        public int map_id;
        public int id;
        public int x;
        public int y;
        public long hp;
        public int sp;
        public int direction;
        public int respawn;
        public int respawn_save;
        public bool dead;
        public int mon_id;
        public bool delete_sw;

        public Monster()
        {
            map_id = 0;
            id = 0;
            x = 0;
            y = 0;
            hp = 0;
            sp = 0;
            direction = 0;
            respawn = 0;
            respawn_save = 0;
            dead = false;
            delete_sw = false;
        }
    }
}
