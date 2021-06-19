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
        public int hp;
        public int direction;
        public int respawn;
        public bool dead;

        public Monster()
        {
            map_id = 0;
            id = 0;
            x = 0;
            y = 0;
            hp = 0;
            direction = 0;
            respawn = 0;
            dead = false;
        }
    }
}
