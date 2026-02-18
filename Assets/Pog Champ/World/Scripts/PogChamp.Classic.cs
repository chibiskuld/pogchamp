using UdonSharp;

namespace MilkCaps
{
    public partial class PogChamp : UdonSharpBehaviour
    {
        private bool CheckWinnerClassic()
        {
            CalculatePool();
            if (pool > 0) return false;

            int w = 0;
            winner = "";
            if (player1 != null)
            {
                if (p1pogs > w)
                {
                    winner = player1.displayName;
                    w = p1pogs;
                }
            }
            if (player2 != null)
            {
                if (p2pogs > w)
                {
                    winner = player2.displayName;
                    w = p2pogs;
                }else if (p2pogs == w)
                {
                    winner += " and " + player2.displayName;
                }
            }
            if (player3 != null)
            {
                if (p3pogs > w)
                {
                    winner = player3.displayName;
                    w = p3pogs;
                }else if (p3pogs == w)
                {
                    winner += " and " + player3.displayName;
                }
            }
            if (player4 != null)
            {
                if (p4pogs > w)
                {
                    winner = player4.displayName;
                    w = p4pogs;
                }else if (p4pogs == w)
                {
                    winner += " and " + player4.displayName;
                }
            }
            gameState = 5;
            return true;
        }

        private void CommitScoreClassic()
        {
            switch(gameState)
            {
                case 1:
                    p1pogs += flipped;
                    break;
                case 2:
                    p2pogs += flipped;
                    break;
                case 3:
                    p3pogs += flipped;
                    break;
                case 4:
                    p4pogs += flipped;
                    break;
            }
            flipped = 0;
        }
    }
}