using UdonSharp;

namespace MilkCaps
{
    public partial class PogChamp : UdonSharpBehaviour
    {
        [UdonSynced] private string winner = "";
        private bool CheckMultiplayerSurvival()
        {
            string possibleWinner = "no winner";
            int alive = 0;

            if (player1 != null && p1pogs > 0) 
            {
                alive++;
                possibleWinner = player1.displayName;
            }
            if (player2 != null && p2pogs > 0) 
            {
                alive++;
                possibleWinner = player2.displayName;
            }
            if (player3 != null && p3pogs > 0) 
            {
                alive++;
                possibleWinner = player3.displayName;
            }
            if (player4 != null && p4pogs > 0) 
            {
                alive++;
                possibleWinner = player4.displayName;
            }
            if (alive < 2)
            {
                winner = possibleWinner;
                return true;
            }
            return false;
        }

        private bool CheckSoloSurvival()
        {
            int pogsRemaining = 0;
            string pWinner = "no winner";
            if (player1 != null) {
                pogsRemaining += p1pogs;
                pWinner = player1.displayName;
            }
            if (player2 != null) {
                pogsRemaining += p2pogs;
                pWinner = player2.displayName;
            }
            if (player3 != null) {
                pogsRemaining += p3pogs;
                pWinner = player3.displayName;
            }
            if (player4 != null) {
                pogsRemaining += p4pogs;
                pWinner = player4.displayName;
            }
            
            if (pogsRemaining > 0) return false;

            winner = pWinner;
            return true;
        }

        //this should only be run as owner, is ran as a part of the score commit.
        private bool CheckWinnerSurvival()
        {
            bool hasWinner;
            if (ActivePlayers() == 1)
            {
                hasWinner = CheckSoloSurvival();
            }
            else
            {
                hasWinner = CheckMultiplayerSurvival();
            }

            if (hasWinner) 
            {
                gameState = 5;
            }
            return hasWinner;
        }

        private void CommitScoreSurvival()
        {
            switch(gameState)
            {
                case 1:
                    p1pogs -= flipped;
                    break;
                case 2:
                    p2pogs -= flipped;
                    break;
                case 3:
                    p3pogs -= flipped;
                    break;
                case 4:
                    p4pogs -= flipped;
                    break;
            }
            flipped = 0;
        }
    }
}