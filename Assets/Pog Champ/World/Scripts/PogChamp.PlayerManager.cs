using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;

namespace MilkCaps
{
    public partial class PogChamp : UdonSharpBehaviour
    {
        [UdonSynced] private string p1Name;
        [UdonSynced] private string p2Name;
        [UdonSynced] private string p3Name;
        [UdonSynced] private string p4Name;
        private VRCPlayerApi player1;
        private bool pPlayer1;
        private VRCPlayerApi player2;
        private bool pPlayer2;
        private VRCPlayerApi player3;
        private bool pPlayer3;
        private VRCPlayerApi player4;
        private bool pPlayer4;

        private VRCPlayerApi[] players = new VRCPlayerApi[0];

        private bool IsPlayer()
        {
            if (Networking.LocalPlayer==null) return false;
            if (ComparePlayers(Networking.LocalPlayer, player1)) return true;
            if (ComparePlayers(Networking.LocalPlayer, player2)) return true;
            if (ComparePlayers(Networking.LocalPlayer, player3)) return true;
            if (ComparePlayers(Networking.LocalPlayer, player4)) return true;
            return false;
        }

        private void EnforceSlammerOwner(GameObject slammer, ref VRCPlayerApi player)
        {
            if (player == null) return;
            if (Networking.IsOwner(player, slammer))
                return;
            Networking.SetOwner(player,slammer);
        }

        private void EnforceSlammerOwners()
        {
            EnforceSlammerOwner(slammer1.gameObject, ref player1);
            EnforceSlammerOwner(slammer2.gameObject, ref player2);
            EnforceSlammerOwner(slammer3.gameObject, ref player3);
            EnforceSlammerOwner(slammer4.gameObject, ref player4);
        }

        private void VerifyPlayer(ref string name, ref VRCPlayerApi player)
        {
            if (string.IsNullOrEmpty(name))
            {
                //no name register, so ensure the API player version is empty.
                player = null;
                return;
            }
            
            //no change, valid state, do nothing
            if (player != null && player.displayName.CompareTo(name) == 0) 
            {
                return;
            }

            //find the player in the player list and register the player.
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null && players[i].displayName.CompareTo(name) == 0)
                {
                    player = players[i];
                }
            }

            //if there is a name, but no existing player, then clear the player name.
            if (Networking.IsOwner(gameObject) && player == null)
            {
                name = "";
            }
        }

        private void VerifyPlayers()
        {
            VerifyPlayer(ref p1Name, ref player1);
            VerifyPlayer(ref p2Name, ref player2);
            VerifyPlayer(ref p3Name, ref player3);
            VerifyPlayer(ref p4Name, ref player4);
        }

        //make sure player is also in the player list
        private void VerifyPlayerOwner(ref string pname, ref VRCPlayerApi player)
        {
            if (string.IsNullOrEmpty(pname)) return;
            
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null && players[i].displayName.CompareTo(pname) == 0)
                {
                    return;
                }
            }

            pname = "";
            player = null;
        }

        //ran as owner: make sure players are in the player list, or remove them from the game.
        private void VerifyPlayersOwner()
        {
            VerifyPlayerOwner(ref p1Name, ref player1);
            VerifyPlayerOwner(ref p2Name, ref player2);
            VerifyPlayerOwner(ref p3Name, ref player3);
            VerifyPlayerOwner(ref p4Name, ref player4);
        }

        private void CheckValidAndRegisterPlayer(string player, ref VRCPlayerApi targetPlayer, ref string pname, GameObject slammer)
        {
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i]!=null && players[i].displayName.CompareTo(player)==0)
                {
                    targetPlayer = players[i];
                    Networking.SetOwner(targetPlayer, slammer.gameObject);
                    pname = player;
                    return;
                }
            }        

            Debug.Log("Warning: Broken instance or Modded client user may be present");
        }
        

        [NetworkCallable]  
        public void RegisterPlayerOwner(string player, int playerN)
        {
            if (!Networking.IsOwner(gameObject)) return;
            if (gameState != 0) return;
            if (playerN < 1 || playerN > 4) return;

            UpdatePlayers();

            switch(playerN)
            {
                case 1:
                    if (string.IsNullOrEmpty(p1Name))
                        CheckValidAndRegisterPlayer(player, ref player1, ref p1Name, slammer1.gameObject);
                    else 
                        p1Name = "";
                    break;
                case 2:
                    if (string.IsNullOrEmpty(p2Name))
                        CheckValidAndRegisterPlayer(player, ref player2, ref p2Name, slammer2.gameObject);
                    else 
                        p2Name = "";
                    break;
                case 3:
                    if (string.IsNullOrEmpty(p3Name))
                        CheckValidAndRegisterPlayer(player, ref player3, ref p3Name, slammer3.gameObject);
                    else 
                        p3Name = "";
                    break;
                case 4:
                    if (string.IsNullOrEmpty(p4Name))
                        CheckValidAndRegisterPlayer(player, ref player4, ref p4Name, slammer4.gameObject);
                    else 
                        p4Name = "";
                    break;
            }
        }

        public void RegisterPlayer1()
        {
            if (gameState != 0) return;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(RegisterPlayerOwner), Networking.LocalPlayer.displayName, 1);
        }

        public void RegisterPlayer2()
        {
            if (gameState != 0) return;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(RegisterPlayerOwner), Networking.LocalPlayer.displayName, 2);
        }

        public void RegisterPlayer3()
        {
            if (gameState != 0) return;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(RegisterPlayerOwner), Networking.LocalPlayer.displayName, 3);
        }

        public void RegisterPlayer4()
        {
            if (gameState != 0) return;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(RegisterPlayerOwner), Networking.LocalPlayer.displayName, 4);
        }

        //maintain the player list
        public bool ComparePlayers(VRCPlayerApi pa, VRCPlayerApi pb)
        {
            if (pa == null && pb == null) return true;
            if (pa == null) return false;
            if (pb == null) return false;
            return pa.displayName.CompareTo(pb.displayName) == 0;
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            SendCustomEventDelayedFrames(nameof(UpdatePlayers),1);
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            SendCustomEventDelayedFrames(nameof(UpdatePlayers),1);
        }        

        public void UpdatePlayers()
        {
            players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];  
            VRCPlayerApi.GetPlayers(players);
        }
    }
}