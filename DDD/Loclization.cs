using Dalamud;
using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDD
{
   
    public static class Loclization
    {
        public static bool isCN
        {
            get
            {
                switch ((int)DalamudApi.ClientState.ClientLanguage)
                {
                    case 4:return true;
                    default:return false;
                }
            }
        }
    }
}
