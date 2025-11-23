using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using System;

namespace MatchZy;

public partial class MatchZy
{
    public int captainNum = 0;
    public bool isHaveCaptain = false;
    public bool isCaptainPicking = false;
    public int leftPlayerNum = 8;
    public Dictionary<int, bool?> isChoised = new Dictionary<int, bool?>();
    public CsTeam lastVetoCaptain = CsTeam.Terrorist;//与makeCaptain()同步，这里默认team2初始阵营为T
   
   public void makeCaptain() 
   {
       if(captainNum >= 2)
       {
           isHaveCaptain = true;
           //先将队长阵营固定。
           playerData[vetoCaptains["team1"]].SwitchTeam(CsTeam.CounterTerrorist);//SwitchTeam: 强制切换玩家队伍，玩家将保持存活并保留武器。
           playerData[vetoCaptains["team2"]].SwitchTeam(CsTeam.Terrorist);
           playerData[vetoCaptains["team2"]].CommitSuicide(explode:false , force:true);
           playerData[vetoCaptains["team1"]].CommitSuicide(explode:false , force:true);
           Server.ExecuteCommand($"mp_teamname_1 {reverseTeamSides["CT"].teamName}");
           Server.ExecuteCommand($"mp_teamname_2 {reverseTeamSides["TERRORIST"].teamName}");
           leftPlayerNum = connectedPlayers - 2;
           PickPlayer();
           return;
           //CreateVeto();         
       }
       Server.PrintToChatAll($"{chatPrefix} 输入 .captain 来抢队长,名额还剩 {2-captainNum} 个。");
    }
        
   public int GetTeamCaptainId(CCSPlayerController player) 
   {
       foreach (var key in playerData.Keys)
       {
           if (!playerData[key].IsValid) continue;
           if (playerData[key] == player) return key;
       }
            
       return -1;
   }        
        
   public void HandleChangeCaptain(CCSPlayerController player, string map) 
   {
       Log($"[HandleChangeCaptain - DEBUG]:captainNum:{captainNum}");
       if(captainNum >= 2) return;
       string team = captainNum == 0 ? "team2" : "team1";
       vetoCaptains[team] = GetTeamCaptainId(player); //key
       Log($"[HandleChangeCaptain - DEBUG]:vetoCaptains[team]:{vetoCaptains[team]}");
       isChoised[vetoCaptains[team]] = true;
       if (captainNum == 0)
       {
           teamSides[matchzyTeam1] = "CT";
           reverseTeamSides["CT"] = matchzyTeam1;
           matchzyTeam1.teamName = "team_" + playerData[vetoCaptains[team]].PlayerName;
       }
       else
       {
           teamSides[matchzyTeam2] = "TERRORIST";
           reverseTeamSides["TERRORIST"] = matchzyTeam2;
           matchzyTeam2.teamName = "team_" + playerData[vetoCaptains[team]].PlayerName;
       }
       captainNum += 1;
       makeCaptain();
   }        

   public void PickPlayer()
   {
       Log($"[PickPlayer - DEBUG]:leftPlayerNum:{leftPlayerNum}");
       if (leftPlayerNum == 0)
       {
           SwapPlayersToTeams();
           CreateVeto();
       }
       else
       {
           CsTeam otherVetoCaptain = lastVetoCaptain;
           if (lastVetoCaptain == CsTeam.Terrorist) otherVetoCaptain = CsTeam.CounterTerrorist;
           else if (lastVetoCaptain == CsTeam.CounterTerrorist) otherVetoCaptain = CsTeam.Terrorist;                
           PromptForPlayerSelectionInChat(otherVetoCaptain);
       }
   }        

   public string getLeftPlayers()
   {
       string res = "";
       int count = 0;
       Log($"[getLeftPlayers - DEBUG]:befor foreach");
       foreach (var key in playerData.Keys)
       {
           count += 1;
           Log($"[getLeftPlayers - DEBUG]:befor isChoised");
           if (!isChoised.ContainsKey(key) || isChoised[key] == null || isChoised[key] == false)
           {
               Log($"[getLeftPlayers - DEBUG]:befor res");
               res = res == "" ? res + "[" + key.ToString() + "]:" + playerData[key].PlayerName: res + " , [" + key.ToString() + "]:" + playerData[key].PlayerName;
               Log($"[getLeftPlayers - DEBUG]:after res");
           }
                
                
       }
       Log($"[getLeftPlayers - DEBUG]:count:{count}");
       return res;
   }        
        
   public void PromptForPlayerSelectionInChat(CsTeam Team)
   {
       string team = Team == CsTeam.CounterTerrorist ? "team1" : "team2";
       string currentCaptainName = playerData[vetoCaptains[team]].PlayerName;
       string leftPlayers = getLeftPlayers();
       Server.PrintToChatAll($"{chatPrefix} 请队长 {ChatColors.Green}{currentCaptainName} 输入 .love <id> 来选择队员。剩余队员有：{leftPlayers}，只输入名字前的编号即可。");
   }        

   public void HandlePickPlayer(CCSPlayerController player, string id)
   {
       string currentTeamToPick = lastVetoCaptain == CsTeam.CounterTerrorist ? "team2" : "team1";
       if (player.UserId != vetoCaptains[currentTeamToPick] || (int.TryParse(id , out _) == false)) return;
       int key = int.Parse(id);
       if (!isChoised.ContainsKey(key) || isChoised[key] == null || isChoised[key] == false)
       {
           int captainId =  GetTeamCaptainId(player);
           if (!playerData.ContainsKey(key))
           {
               PrintToAllChat($"无效ID!");
               return;
           }
           playerData[key].SwitchTeam(playerData[captainId].Team);
           isChoised[key] = true;
           lastVetoCaptain = currentTeamToPick == "team1" ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
           leftPlayerNum -= 1;
           PickPlayer();
       }
            
   }        
                 
}