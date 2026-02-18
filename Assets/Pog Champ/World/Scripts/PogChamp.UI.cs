using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace MilkCaps
{
    public partial class PogChamp : UdonSharpBehaviour
    {
        [SerializeField] private Sprite joinSprite;
        [SerializeField] private Sprite leaveSprite;
        [SerializeField] private Image player1Button;
        [SerializeField] private TMP_Text player1Name;
        [SerializeField] private TMP_Text player1Pogs;
        [SerializeField] private GameObject player1Indicator;
        
        [SerializeField] private Image player2Button;
        [SerializeField] private TMP_Text player2Name;
        [SerializeField] private TMP_Text player2Pogs;
        [SerializeField] private GameObject player2Indicator;

        [SerializeField] private Image player3Button;
        [SerializeField] private TMP_Text player3Name;
        [SerializeField] private TMP_Text player3Pogs;
        [SerializeField] private GameObject player3Indicator;
        
        [SerializeField] private Image player4Button;
        [SerializeField] private TMP_Text player4Name;
        [SerializeField] private TMP_Text player4Pogs;
        [SerializeField] private GameObject player4Indicator;

        [SerializeField] private GameObject startUI;
        [SerializeField] private GameObject gameUI;
        [SerializeField] private GameObject gameOverUI;

        [SerializeField] private GameObject gameModes;
        [SerializeField] private Button gmSurvival;
        [SerializeField] private Button gmRace;
        [SerializeField] private Button gmClassic;

        [SerializeField] private Button startButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private TMP_Text resetText;
        [SerializeField] private TMP_Text currentTurn;
        [SerializeField] private GameObject ResultsUI;
        [SerializeField] private TMP_Text flippedText;
        [SerializeField] private TMP_Text nextRound;
        [SerializeField] private Button skipButton;

        [SerializeField] private Animator uiAnimator;
        [SerializeField] private TMP_Text winnerText;
        [SerializeField] private TMP_Text rulesText;

        private bool isPlayer = false;

        private void RenderUI()
        {
            //ActivePlayers() > 0 && IsPlayer()
            isPlayer = IsPlayer();
            startButton.interactable = isPlayer;

            gameModes.SetActive(isPlayer);
            gmSurvival.interactable = isPlayer;
            gmRace.interactable = isPlayer;
            gmClassic.interactable = isPlayer;
            resetButton.gameObject.SetActive(isPlayer);

            player1Indicator.SetActive(gameState==1);
            player2Indicator.SetActive(gameState==2);
            player3Indicator.SetActive(gameState==3);
            player4Indicator.SetActive(gameState==4);

            switch(gameState)
            {
                case 0:
                    RenderStartUI();
                    break;
                case 1:
                case 2:
                case 3:
                case 4:
                    RenderGameUI();
                    break;
                case 5:
                    RenderGameOverUI();
                    break;
            }
            RenderDebug();
            RenderResetTimeout();
        }

        private void RenderStartUI()
        {
            startUI.SetActive(true);
            gameUI.SetActive(false);
            gameOverUI.SetActive(false);
            player1Button.sprite = string.IsNullOrEmpty(p1Name) ? joinSprite : leaveSprite;
            player2Button.sprite = string.IsNullOrEmpty(p2Name) ? joinSprite : leaveSprite;
            player3Button.sprite = string.IsNullOrEmpty(p3Name) ? joinSprite : leaveSprite;
            player4Button.sprite = string.IsNullOrEmpty(p4Name) ? joinSprite : leaveSprite;

            player1Name.text = (player1 != null) ? player1.displayName : "(No Entry)";
            player2Name.text = (player2 != null) ? player2.displayName : "(No Entry)";
            player3Name.text = (player3 != null) ? player3.displayName : "(No Entry)";
            player4Name.text = (player4 != null) ? player4.displayName : "(No Entry)";

            uiAnimator.SetInteger("gamemode",gameMode);

            if (isPlayer)
            {
                switch(gameMode)
                {
                    case 0:
                        rulesText.text = "Everyone starts with 100 pogs. Players take turns slamming their stack, until none of their pogs are left. Last player with pogs left wins.";
                        break;
                    case 1:
                        rulesText.text = "Everyone starts with 100 pogs. Players take turns slamming their stack. First player to flip all of their pogs, wins.";
                        break;
                    case 2:
                        rulesText.text = "Every player adds 25 pogs to the pool, players take turns slamming the stack until all pogs are flipped. Players collect any pogs they flip and the one with the most at the end wins.";
                        break;
                }
            }
            else
            {
                rulesText.text = "Click Join!";
            }
        }

        private void RenderResults(VRCPlayerApi player)
        {
            ResultsUI.SetActive(true);
            currentTurn.gameObject.SetActive(false);
            
            flippedText.text = (player != null) ? $"{player.displayName} flipped {flipped}!" : "(No player)";
            skipButton.gameObject.SetActive(ComparePlayers(Networking.LocalPlayer,player));
            nextRound.text = $"Next player in {(int)resultsTR}s";
        } 

        private void RenderCurrentTurn(VRCPlayerApi player)
        {
            ResultsUI.SetActive(false);
            currentTurn.gameObject.SetActive(true);

            currentTurn.text = (player != null) ? $"It's {player.displayName}'s turn!" : "(No player)";
            if (gameMode==2)
            {
                currentTurn.text += $"\n{pool} left";
            }
        }

        private void RenderGameUIPlayer(VRCPlayerApi player)
        {
            if(roundEnded)
            {
                RenderResults(player);
            }
            else
            {
                RenderCurrentTurn(player);
            }
        }

        private void RenderGameUI()
        {
            startUI.SetActive(false);
            gameUI.SetActive(true);
            gameOverUI.SetActive(false);

            uiAnimator.SetInteger("player", gameState);

            player1Pogs.text = (player1 != null) ? p1pogs.ToString() : "";
            player2Pogs.text = (player2 != null) ? p2pogs.ToString() : "";
            player3Pogs.text = (player3 != null) ? p3pogs.ToString() : "";
            player4Pogs.text = (player4 != null) ? p4pogs.ToString() : "";

            player1Name.text = (player1 != null) ? player1.displayName : "";
            player2Name.text = (player2 != null) ? player2.displayName : "";
            player3Name.text = (player3 != null) ? player3.displayName : "";
            player4Name.text = (player4 != null) ? player4.displayName : "";

            switch(gameState)
            {
                case 1:
                    RenderGameUIPlayer(player1);
                    break;
                case 2:
                    RenderGameUIPlayer(player2);
                    break;
                case 3:
                    RenderGameUIPlayer(player3);
                    break;
                case 4:
                    RenderGameUIPlayer(player4);
                    break;
            }
        }

        private void RenderGameOverUI()
        {
            startUI.SetActive(false);
            gameUI.SetActive(false);
            gameOverUI.SetActive(true);


            player1Pogs.text = "";
            player2Pogs.text = "";
            player3Pogs.text = "";
            player4Pogs.text = "";

            player1Name.text = "";
            player2Name.text = "";
            player3Name.text = "";
            player4Name.text = "";
            winnerText.text = winner;
        }

        [SerializeField] private TMP_Text debugText;
        private string GetOwnerName(GameObject gameObject)
        {
            string output = "(null)";
            VRCPlayerApi test = Networking.GetOwner(gameObject);
            if (test==null) return output;
            return test.displayName;
        }
        private void RenderDebug()
        {
            string output = "";
            output += $"Owner: {GetOwnerName(gameObject)}\n";
            output += $"Slammer 1 owner: {GetOwnerName(slammer1.gameObject)}\n";
            output += $"Slammer 2 owner: {GetOwnerName(slammer2.gameObject)}\n";
            output += $"Slammer 3 owner: {GetOwnerName(slammer3.gameObject)}\n";
            output += $"Slammer 4 owner: {GetOwnerName(slammer4.gameObject)}\n";
            output += $"Player 1: {p1Name}\n";
            if (player1 != null) output += $"  API: {player1.displayName}\n";
            output += $"Player 2: {p2Name}\n";
            if (player2 != null) output += $"  API: {player2.displayName}\n";
            output += $"Player 3: {p3Name}\n";
            if (player3 != null) output += $"  API: {player3.displayName}\n";
            output += $"Player 4: {p4Name}\n";
            if (player4 != null) output += $"  API: {player4.displayName}\n";
            output += $"player 1 pogs: {p1pogs}\n";
            output += $"player 2 pogs: {p2pogs}\n";
            output += $"player 3 pogs: {p3pogs}\n";
            output += $"player 4 pogs: {p4pogs}\n";
            output += $"Pog Pool: {pool}\n";
            output += $"Game Mode: {gameMode}\n";
            output += $"Game State: {gameState}\n";
            output += $"Round Ended: {roundEnded}\n";
            output += $"Seed: {seed}\n";
            output += $"Flipped: {flipped}\n";
            output += $"Winner: {winner}\n";
            output += $"Inactivity Reset: {timeRemaining}\n";
            output += $"Players in the room:\n";
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i]!=null) output += $"{players[i].displayName}\n";
            }

            debugText.text = output;
        }

        private void RenderResetTimeout()
        {
            if (timeRemaining < 10)
            {
                resetText.gameObject.SetActive(true);
                resetText.text = $"Inactivity Reset in {(int)timeRemaining}s";
            }
            else
            {
                resetText.gameObject.SetActive(false);
            }
        }
    }
}