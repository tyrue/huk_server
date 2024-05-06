using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupremePlayServer
{
    public class Item
    {
        public int id;
        public int map_id;
        public int x;
        public int y;
        public int type; // 아이템 종류(0 아이템, 1 무기, 2 장비, 3돈)
        public int item_id;
        public int num; // 드랍 갯수
        public int sw; // 필요한 스위치
    }
}
