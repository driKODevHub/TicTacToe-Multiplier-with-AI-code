using UnityEngine;
using System.Collections.Generic;

public class AIPlayer : MonoBehaviour
{
    // ... весь ваш існуючий код AIPlayer.cs залишається тут без змін ...
    // Він не залежить від мережі, тому його не потрібно оновлювати.

    // Minimax algorithm implementation
    private static int Minimax(int[,] board, int depth, bool isMaximizing, int playerMark, int opponentMark)
    {
        int score = EvaluateBoard(board);

        if (score == 10) return score - depth;
        if (score == -10) return score + depth;
        if (!IsMovesLeft(board)) return 0;

        if (isMaximizing)
        {
            int best = -1000;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (board[i, j] == 0)
                    {
                        board[i, j] = playerMark;
                        best = Mathf.Max(best, Minimax(board, depth + 1, !isMaximizing, playerMark, opponentMark));
                        board[i, j] = 0;
                    }
                }
            }
            return best;
        }
        else
        {
            int best = 1000;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (board[i, j] == 0)
                    {
                        board[i, j] = opponentMark;
                        best = Mathf.Min(best, Minimax(board, depth + 1, !isMaximizing, playerMark, opponentMark));
                        board[i, j] = 0;
                    }
                }
            }
            return best;
        }
    }

    // Finds the best move for the AI
    public int FindBestMove(int[,] board, int playerMark, int opponentMark, float errorPercentage)
    {
        // Chance to make a random "mistake"
        if (Random.Range(0f, 100f) < errorPercentage)
        {
            List<int> emptyCells = new List<int>();
            for (int i = 0; i < 9; i++)
            {
                if (board[i / 3, i % 3] == 0)
                {
                    emptyCells.Add(i);
                }
            }
            if (emptyCells.Count > 0)
            {
                return emptyCells[Random.Range(0, emptyCells.Count)];
            }
        }

        int bestVal = -1001;
        int bestMoveIndex = -1;

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (board[i, j] == 0)
                {
                    board[i, j] = playerMark;
                    int moveVal = Minimax(board, 0, false, playerMark, opponentMark);
                    board[i, j] = 0;

                    if (moveVal > bestVal)
                    {
                        bestMoveIndex = i * 3 + j;
                        bestVal = moveVal;
                    }
                }
            }
        }
        return bestMoveIndex;
    }

    // Checks if there are moves left on the board
    private static bool IsMovesLeft(int[,] board)
    {
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                if (board[i, j] == 0) return true;
        return false;
    }

    // Evaluates the board state
    public static int EvaluateBoard(int[,] b)
    {
        // Checking for Rows for X or O victory.
        for (int row = 0; row < 3; row++)
        {
            if (b[row, 0] == b[row, 1] && b[row, 1] == b[row, 2])
            {
                if (b[row, 0] == 1) return -10; // X wins
                else if (b[row, 0] == 2) return 10; // O wins
            }
        }

        // Checking for Columns for X or O victory.
        for (int col = 0; col < 3; col++)
        {
            if (b[0, col] == b[1, col] && b[1, col] == b[2, col])
            {
                if (b[0, col] == 1) return -10;
                else if (b[0, col] == 2) return 10;
            }
        }

        // Checking for Diagonals for X or O victory.
        if (b[0, 0] == b[1, 1] && b[1, 1] == b[2, 2])
        {
            if (b[0, 0] == 1) return -10;
            else if (b[0, 0] == 2) return 10;
        }

        if (b[0, 2] == b[1, 1] && b[1, 1] == b[2, 0])
        {
            if (b[0, 2] == 1) return -10;
            else if (b[0, 2] == 2) return 10;
        }

        // Check winner mark for network game
        if (IsMovesLeft(b) == false) return 0; // Draw

        // For multiplayer winner check
        int winner = CheckWinnerMark(b);
        if (winner != 0) return winner == 1 ? -10 : 10;


        return 0; // No winner yet
    }

    public static int CheckWinnerMark(int[,] board)
    {
        // Rows
        for (int i = 0; i < 3; i++)
        {
            if (board[i, 0] != 0 && board[i, 0] == board[i, 1] && board[i, 1] == board[i, 2]) return board[i, 0];
        }
        // Columns
        for (int i = 0; i < 3; i++)
        {
            if (board[0, i] != 0 && board[0, i] == board[1, i] && board[1, i] == board[2, i]) return board[0, i];
        }
        // Diagonals
        if (board[0, 0] != 0 && board[0, 0] == board[1, 1] && board[1, 1] == board[2, 2]) return board[0, 0];
        if (board[0, 2] != 0 && board[0, 2] == board[1, 1] && board[1, 1] == board[2, 0]) return board[0, 2];

        return 0; // No winner
    }
}

