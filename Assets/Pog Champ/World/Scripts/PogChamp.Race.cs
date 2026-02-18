using UdonSharp;

namespace MilkCaps
{
    public partial class PogChamp : UdonSharpBehaviour
    {
        private bool CheckWinnerRace()
        {
            if (player1 != null && p1pogs < 1)
            {
                winner = player1.displayName;
                gameState = 5;
                return true;
            }
            if (player2 != null && p2pogs < 1)
            {
                winner = player2.displayName;
                gameState = 5;
                return true;
            }
            if (player3 != null && p3pogs < 1)
            {
                winner = player3.displayName;
                gameState = 5;
                return true;
            }
            if (player4 != null && p4pogs < 1)
            {
                winner = player4.displayName;
                gameState = 5;
                return true;
            }
            return false;
        }

        private void CommitScoreRace()
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