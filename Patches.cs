using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
namespace InSongLeaderboard
{
    [HarmonyPatch(typeof(LeaderboardTableView))]
    [HarmonyPatch("SetScores", MethodType.Normal)]
    class LeaderboardTableViewSetScores
    {
        static void Postfix(List<LeaderboardTableView.ScoreData> scores, int specialScorePos)
        {
            Plugin.GrabScores();
        }
    }
}
