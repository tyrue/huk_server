using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupremePlayServer
{
    public class Item
    {
        public int map_id;
        public int id;
        public int x;
        public int y;
    }

    public class Item2
    {
        public int d_id;
        public int map_id;
        public int x;
        public int y;
        public int type1; // 아이템 종류(0 아이템, 1 무기, 2 장비)
        public int type2; // 돈(0), 아이템 종류(1)
        public int id;
        public int num; // 드랍 갯수
    }
}
