using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupremePlayServer
{
    public class EventNpc
    {
        public int map_id;
        public int id;
        public int x;
        public int y;
        public int direction;
        public int npc_id;

        public EventNpc()
        {
            map_id = 0;
            id = 0;
            x = 0;
            y = 0;
            direction = 0;
            npc_id = 0;
        }
    }
}
