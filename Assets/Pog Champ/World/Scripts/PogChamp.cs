using System;
using UdonSharp;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;

namespace MilkCaps
{
    public partial class PogChamp : UdonSharpBehaviour
    {
        [SerializeField] public float resultsTime = 10;
        [SerializeField] private Animator animator;
        [SerializeField] private Renderer pogs;

        [SerializeField] private Slammer slammer1;
        [SerializeField] private Slammer slammer2;
        [SerializeField] private Slammer slammer3;
        [SerializeField] private Slammer slammer4;
        /*
            Collect Mode / Official Rules:
                There official rule, is up to 4 players contribute pogs
                (We can add 25 for each player if we add that mode)
            
            Survival Mode / Typical House Rule:
                players start with 100 each, and play until all but 1 have busted.
                (There's no possibility of a tie in survival mode)

            Race / Typical House Rule:
                players start with 100 each, and player to reach 0 first wins.
                (There's no possibility of a tie in race mode)
        */

        [UdonSynced] private int gameMode = 0;
        [UdonSynced] private int gameState = 0;
        private int pGameState = 0;
        [UdonSynced] private float seed = 0;
        /*
            0 = not started
            1 = Player 1 turn
            2 = Player 2 turn
            3 = Player 3 turn
            4 = Player 4 turn
            5 = Game Over
        */
        [UdonSynced] private int p1pogs = 100;
        [UdonSynced] private int p2pogs = 100;
        [UdonSynced] private int p3pogs = 100;
        [UdonSynced] private int p4pogs = 100;
        private int pool = 0; //calculated, no reason to waste sync.
        [UdonSynced] private int flipped = 0;
        [UdonSynced] private bool roundEnded = false;
        private DateTime resultTime = DateTime.Now;
        private float resultsTR = 10;

        //sounds
        [SerializeField] private AudioSource SEPogSplash;
        [SerializeField] private AudioSource SEGameStart;
        [SerializeField] private AudioSource SEPlayerEnter;
        [SerializeField] private AudioSource SEPlayerLeave;
        [SerializeField] private AudioSource SENextTurn;
        [SerializeField] private AudioSource SEYourTurn;
        [SerializeField] private AudioSource SEGameOverLeft;
        [SerializeField] private AudioSource SEGameOverRight;
        private DateTime stateStartTime = DateTime.Now;
        private float timeRemaining = 0;

        private void Start()
        {
            Debug.Log("= POG Champ System Starting =");
            pGameState = gameState;

            SlowUpdate();
            UpdatePlayers();
            if (Networking.IsOwner(gameObject))
            {
                pool = 100;
                seed = UnityEngine.Random.Range(0.0f, 1.0f);

                pogs.sharedMaterial.SetFloat("_PogSeed",seed);
                pogs.sharedMaterial.SetFloat("_Pogs",pool);            
            }
            pogs.sharedMaterial.SetFloat("_PogAnimation",0.0f);
        }

        public override void OnDeserialization()
        {
            pogs.sharedMaterial.SetFloat("_PogSeed",seed);
            pogs.sharedMaterial.SetFloat("_Pogs",pool);            
        }

        void OnDisable()
        {
            Debug.Log("= POG Champ System Disabled =");            
        }

        public void SlowUpdate()
        {
            //integrity Check.
            if (gameState < 0 || gameState > 5)
            {
                Reset();
            }
            else 
            {
                SlowUpdateHandler();
            }
            SendCustomEventDelayedSeconds("SlowUpdate",.1f);
        }

        private void SlowUpdateHandler()
        {
            RenderUI();
            VerifyPlayers();
            ConfigureSlammer(slammer1, Networking.IsOwner(slammer1.gameObject) && gameState == 1);
            ConfigureSlammer(slammer2, Networking.IsOwner(slammer2.gameObject) && gameState == 2);
            ConfigureSlammer(slammer3, Networking.IsOwner(slammer3.gameObject) && gameState == 3);
            ConfigureSlammer(slammer4, Networking.IsOwner(slammer4.gameObject) && gameState == 4);
            if (roundEnded)
            {
                TimeSpan ts = DateTime.Now - resultTime;
                resultsTR = resultsTime - (float)ts.TotalSeconds;
            }

            //Ensure A player is owner checks if the object is owned by a player, but also
            //ensures that a player is set to an owner, if they are not.
            if (Networking.IsOwner(gameObject))
            {
                VerifyPlayersOwner();
                if (ActivePlayers() < 1 && gameState > 0) Reset();
            
                if (gameState > 0 && gameState < 5) 
                {
                    CheckAllHaveQuit();
                    if (roundEnded && resultsTR < 0)
                    {
                        SetNextPlayer();
                    }            
                    if (!ValidPlayer()) SetNextPlayer();
                }
                else
                {
                    roundEnded = false;
                }
            }
            if (gameState > 0 && gameState < 5)
            {
                EnforcePlayerOwnership();
                EnforceSlammerOwners();
            }

            //we set this every update, in case there's lag in syncing.
            pogs.sharedMaterial.SetFloat("_PogSeed",seed);
            SetShaderPogs();
            if (pGameState != gameState)
            {
                DoGameStarted();
                DoGameOver();
            }
            DoPlayerLeaveJoin();
            DoResetTimeout();

            pGameState = gameState;
            pPlayer1 = player1 != null;
            pPlayer2 = player2 != null;
            pPlayer3 = player3 != null;
            pPlayer4 = player4 != null;
        }

        private void DoResetTimeout()
        {
            if (gameState == 0) 
            {
                timeRemaining = 60;
                return;
            }

            //reset timestamp if state changed.
            if (pGameState != gameState) stateStartTime = DateTime.Now;
            if (gameState > 0 && gameState < 5 && roundEnded) stateStartTime = DateTime.Now;

            //on any screen but the start screen, reset if noone interacts after 1 minute.
            TimeSpan ts = DateTime.Now - stateStartTime;
            timeRemaining = 60 - (float)(DateTime.Now - stateStartTime).TotalSeconds;
            
            if (!Networking.IsOwner(gameObject)) return;
            
            if (timeRemaining < 0) Reset();
        }

        private void DoGameStarted()
        {
            if (pGameState != 0) return; 
            if (gameState < 1 || gameState > 4) return;
            
            SEGameStart.Play();
        }

        private void DoGameOver()
        {
            if (pGameState == 0) return;
            if (gameState != 5) return;

            SEGameOverLeft.Play();
            SEGameOverRight.Play();
        }

        //react to the state of a player leaving or joining the game.
        private void DoPlayerLeaveJoin()
        {
            if (player1 == null && pPlayer1) SEPlayerLeave.Play();
            if (player2 == null && pPlayer2) SEPlayerLeave.Play();
            if (player3 == null && pPlayer3) SEPlayerLeave.Play();
            if (player4 == null && pPlayer4) SEPlayerLeave.Play();

            if (player1 != null && !pPlayer1) SEPlayerEnter.Play();
            if (player2 != null && !pPlayer2) SEPlayerEnter.Play();
            if (player3 != null && !pPlayer3) SEPlayerEnter.Play();
            if (player4 != null && !pPlayer4) SEPlayerEnter.Play();
        }

        private void ConfigureSlammer(Slammer slammer, bool state)
        {
            if (slammer.pickup.pickupable!=state) slammer.pickup.pickupable = state;
        }

        private void CheckAllHaveQuit()
        {
            if (player1!=null) return;
            if (player2!=null) return;
            if (player3!=null) return;
            if (player4!=null) return;
            Reset();
        }

        private void EnforcePlayerOwnership()
        {
            if (player1 != null)
            {
                if (!Networking.IsOwner(player1, gameObject))
                {
                    Networking.SetOwner(player1, gameObject);
                }
                return;
            }
            if (player2 != null)
            {
                if (!Networking.IsOwner(player2, gameObject))
                {
                    Networking.SetOwner(player2, gameObject);
                }
                return;
            }
            if (player3 != null) 
            {
                if (!Networking.IsOwner(player3, gameObject))
                {
                    Networking.SetOwner(player3, gameObject);
                }
                return;
            }
            if (player4 != null) 
            {
                if (!Networking.IsOwner(player4, gameObject))
                {
                    Networking.SetOwner(player4, gameObject);
                }
                return;
            }
        }


        private bool PogFlipped(float x, float y)
        {
            bool result = false;
            float rx = Mathf.PI * x * 10;
            float ry = Mathf.PI * y * 10;
            if (rx % (Mathf.PI*2) > Mathf.PI) result = !result;
            if (ry % (Mathf.PI*2) > Mathf.PI) result = !result;

            return result;
        }

        private int PogsFlipped(int pogs)
        {
            int count = 0;

            for (int pogn = 1; pogn < pogs + 1; pogn++)
            {
                float r = 7 + pogn + seed * 13.333f;
                float x, y = 0;
                x = Mathf.Sin(r);
                y = Mathf.Cos(r);
                
                float d = Mathf.Sin(pogn * (3 + pogn + seed * 13.333f)) * 2;
                x *= d;
                y *= d;

                if (PogFlipped(x,y))
                {
                    count++;
                }
            }   
            return count;
        }

        //returns true if the collision is by a player and is the valid player.
        private bool CheckPlayerCollision(VRCPlayerApi contactPlayer, VRCPlayerApi player)
        {
            if (contactPlayer == null) return false; //not a player collision, so true.
            if (player == null) return false;
            return ComparePlayers(contactPlayer, player);
        }

        private bool CheckTags(string[] tags, string pTag)
        {
            foreach(string tag in tags)
            {
                if (tag.CompareTo(pTag) == 0) return true;
                if (tag.CompareTo("PCSLAMMER") == 0) return true;
            }
            
            return false;
        }
        
        public override void OnContactEnter(ContactEnterInfo contactEnterInfo)
        {
            if (roundEnded) return;
            //did the collision happen by this player, and is it their turn.
            switch(gameState)
            {
                case 1:
                    if (!ComparePlayers(Networking.LocalPlayer, player1)) return;
                    if (ComparePlayers(contactEnterInfo.contactSender.player, player1)) break;
                    if (CheckTags(contactEnterInfo.matchingTags, "P1PCSLAMMER")) break;
                    return;
                case 2:
                    if (!ComparePlayers(Networking.LocalPlayer, player2)) return;
                    if (ComparePlayers(contactEnterInfo.contactSender.player, player2)) break;
                    if (CheckTags(contactEnterInfo.matchingTags, "P2PCSLAMMER")) break;
                    return;
                case 3:
                    if (!ComparePlayers(Networking.LocalPlayer, player3)) return;
                    if (ComparePlayers(contactEnterInfo.contactSender.player, player3)) break;
                    if (CheckTags(contactEnterInfo.matchingTags, "P3PCSLAMMER")) break;
                    return;
                case 4:
                    if (!ComparePlayers(Networking.LocalPlayer, player4)) return;
                    if (ComparePlayers(contactEnterInfo.contactSender.player, player4)) break;
                    if (CheckTags(contactEnterInfo.matchingTags, "P4PCSLAMMER")) break;
                    return;
                default:
                    return;
            }
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ContactEnterLocal));
        }

        [NetworkCallable] 
        public void ContactEnterLocal()
        {
            if (roundEnded) return;

            animator.Play("Explode");
            SEPogSplash.Play();
            resultTime = DateTime.Now;

            if (Networking.IsOwner(gameObject))
            {
                if (gameMode == 2)
                {
                    CalculatePool();
                    flipped = PogsFlipped(pool);
                }
                else
                {
                    switch(gameState)
                    {
                        case 1:
                            flipped = PogsFlipped(p1pogs);
                            break;
                        case 2:
                            flipped = PogsFlipped(p2pogs);
                            break;
                        case 3:
                            flipped = PogsFlipped(p3pogs);
                            break;
                        case 4:
                            flipped = PogsFlipped(p4pogs);
                            break;
                    }
                }
                roundEnded = true;
            }
        }

        private void CalculatePool()
        {
            int p = 0;
            if (player1!=null) {
                p += 25;
                p -= p1pogs;
            }
            if (player2!=null) {
                p += 25;
                p -= p2pogs;
            }
            if (player3!=null) {
                p += 25;
                p -= p3pogs;
            }
            if (player4!=null) {
                p += 25;
                p -= p4pogs;
            }

            pool = p;
        }

        private void SetShaderPogs()
        {
            if (gameMode == 2)
            {
                CalculatePool();
                pogs.sharedMaterial.SetInt("_Pogs",pool);
            }
            else
            {
                switch(gameState)
                {
                    case 1:
                        pogs.sharedMaterial.SetInt("_Pogs",p1pogs);
                        break;
                    case 2:
                        pogs.sharedMaterial.SetInt("_Pogs",p2pogs);
                        break;
                    case 3:
                        pogs.sharedMaterial.SetInt("_Pogs",p3pogs);
                        break;
                    case 4:
                        pogs.sharedMaterial.SetInt("_Pogs",p4pogs);
                        break;
                }        
            }
        }

        private void DoNotification(int nextPlayer)
        {
            switch(nextPlayer)
            {
                case 1:
                    if (ComparePlayers(player1, Networking.LocalPlayer)) 
                    {
                        SEYourTurn.Play();
                        return;
                    }
                    break;
               case 2:
                    if (ComparePlayers(player2, Networking.LocalPlayer)) 
                    {
                        SEYourTurn.Play();
                        return;
                    }
                    break;
               case 3:
                    if (ComparePlayers(player3, Networking.LocalPlayer)) 
                    {
                        SEYourTurn.Play();
                        return;
                    }
                    break;
               case 4:
                    if (ComparePlayers(player4, Networking.LocalPlayer)) 
                    {
                        SEYourTurn.Play();
                        return;
                    }
                    break;
            }
                        
            SENextTurn.Play();
        }

        //this can be called before gameState has a chance to set.
        [NetworkCallable] 
        public void SetupPogsForNextPlayerLocal(int nextPlayer)
        {        
            slammer1.DoRespawn();
            slammer2.DoRespawn();
            slammer3.DoRespawn();
            slammer4.DoRespawn();
            animator.Play("Idle");
            pogs.sharedMaterial.SetFloat("_PogSeed",seed);
            SetShaderPogs();

            //this works because roundEnded should be false from the start screen.
            if (roundEnded)
            {
                DoNotification(nextPlayer);
            }
            roundEnded = false;
        }

        public void SkipResults()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SkipResultsOwner));
        }

        [NetworkCallable]
        public void SkipResultsOwner()
        {
            if (!Networking.IsOwner(gameObject)) return;
            if (!roundEnded) return;
            SetNextPlayer();
        }

        //call only by owner
        private void SetNextPlayer()
        {
            switch(gameMode)
            {
                case 0:
                    CommitScoreSurvival();
                    if(CheckWinnerSurvival()) return;
                    break;
                case 1:
                    CommitScoreRace();
                    if(CheckWinnerRace()) return;
                    break;
                case 2:
                    CommitScoreClassic();
                    if(CheckWinnerClassic()) return;
                    break;
            }

            do {
                gameState++;
                if (gameState > 4) gameState = 1;
            } while(!ValidPlayer());
            seed = UnityEngine.Random.Range(0.0f, 1.0f);
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetupPogsForNextPlayerLocal),gameState);
        }

        [NetworkCallable]
        public void StartGameOwner()
        {
            EnforcePlayerOwnership();
            if (!Networking.IsOwner(gameObject)) return;
            if (gameState != 0) return;
            if (ActivePlayers() < 1) return;
                        
            SetNextPlayer();
        }

        public void StartGame()
        {
            //only a registered player can start!
            if (!ComparePlayers(Networking.LocalPlayer, player1) &&
                !ComparePlayers(Networking.LocalPlayer, player2) &&
                !ComparePlayers(Networking.LocalPlayer, player3) &&
                !ComparePlayers(Networking.LocalPlayer, player4)) return;
            
            EnforcePlayerOwnership();
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(StartGameOwner));
        }

        private bool ValidPlayer()
        {
            switch(gameState)
            {
                case 1:
                    if (player1!=null && gameMode == 2) return true;
                    return player1!=null && p1pogs > 0;
                case 2:
                    if (player2!=null && gameMode == 2) return true;
                    return player2!=null && p2pogs > 0;
                case 3:
                    if (player3!=null && gameMode == 2) return true;
                    return player3!=null && p3pogs > 0;
                case 4:
                    if (player4!=null && gameMode == 2) return true;
                    return player4!=null && p4pogs > 0;
            }
            return false;
        }

        public void Reset()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ResetAnyone));            
        }

        [NetworkCallable]
        public void ResetAnyone()
        {
            p1Name = "";
            p2Name = "";
            p3Name = "";
            p4Name = "";
            player1 = null;
            player2 = null;
            player3 = null;
            player4 = null;
            gameState = 0;
            p1pogs = 100;
            p2pogs = 100;
            p3pogs = 100;
            p4pogs = 100;
            gameMode = 0;
            pool = 0;
            roundEnded = false;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ResetLocal));
        }

        [NetworkCallable]  
        public void ResetLocal()
        {    
            animator.Play("Idle");
        } 

        private int ActivePlayers()
        {
            int players = 0;
            if (player1 != null) players++;
            if (player2 != null) players++;
            if (player3 != null) players++;
            if (player4 != null) players++;
            return players;
        }

        public void SetGameModeSurvival()
        {
            //only a registered player can start!
            if (Networking.LocalPlayer!=player1 && 
                Networking.LocalPlayer!=player2 &&
                Networking.LocalPlayer!=player3 &&
                Networking.LocalPlayer!=player4) return;

            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetGameMode), 0);
        }
        public void SetGameModeRace()
        {
            //only a registered player can start!
            if (Networking.LocalPlayer!=player1 && 
                Networking.LocalPlayer!=player2 &&
                Networking.LocalPlayer!=player3 &&
                Networking.LocalPlayer!=player4) return;

            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetGameMode), 1);

        }
        public void SetGameModeClassic()
        {
            //only a registered player can start!
            if (Networking.LocalPlayer!=player1 && 
                Networking.LocalPlayer!=player2 &&
                Networking.LocalPlayer!=player3 &&
                Networking.LocalPlayer!=player4) return;

            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetGameMode), 2);

        }
        
        [NetworkCallable]
        public void SetGameMode(int mode)
        {
            if (!Networking.IsOwner(gameObject)) return;
            if (gameState > 0) return;

            gameMode = mode;
            if (gameMode == 2)
            {
                p1pogs = 0;
                p2pogs = 0;
                p3pogs = 0;
                p4pogs = 0;
            }
            else
            {
                p1pogs = 100;
                p2pogs = 100;
                p3pogs = 100;
                p4pogs = 100;
            }
        }
    }
}