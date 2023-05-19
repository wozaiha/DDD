using Dalamud;
using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDD
{
   
    public static class JudgeLanguage
    {
        public static ClientLanguage Language { get; private set; }
        public static bool isInChina { get; private set; }
        public static void NewJudgeLanguage()
        {
            Language = DalamudApi.ClientState.ClientLanguage;
            
            switch (Language)
            {
                case ClientLanguage.Japanese:
                case ClientLanguage.English:
                case ClientLanguage.German:
                case ClientLanguage.French:
                    isInChina = false;
                    break;
                default:
                    isInChina = true;
                    break;
            }
        }
    }
}
