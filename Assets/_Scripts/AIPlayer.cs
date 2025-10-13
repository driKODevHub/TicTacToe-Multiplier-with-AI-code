using UnityEngine;
using System.Collections.Generic;

public class AIPlayer : MonoBehaviour
{
    /// <summary>
    /// ��������� ��������� ��� ��� ز, �������������� �������� ̳�����.
    /// </summary>
    /// <param name="board">�������� ���� �����</param>
    /// <param name="aiMark">Գ���� ز (1 ��� 2)</param>
    /// <param name="playerMark">Գ���� ������ (1 ��� 2)</param>
    /// <param name="errorPercentage">���� ������� �������</param>
    /// <returns>������ �������� ������� (0-8)</returns>
    public int FindBestMove(int[,] board, int aiMark, int playerMark, float errorPercentage)
    {
        // ��������� ����� �� �������
        if (Random.Range(0f, 100f) < errorPercentage)
        {
            return FindRandomMove(board);
        }

        int bestScore = int.MinValue;
        int bestMove = -1;

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                // ����������, �� ������� �����
                if (board[i, j] == 0)
                {
                    board[i, j] = aiMark; // ������ ���
                    int score = Minimax(board, 0, false, aiMark, playerMark);
                    board[i, j] = 0; // ��������� ���

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = i * 3 + j;
                    }
                }
            }
        }
        return bestMove;
    }

    /// <summary>
    /// ��������� ��������� ����� �������.
    /// </summary>
    private int FindRandomMove(int[,] board)
    {
        List<int> availableMoves = new List<int>();
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (board[i, j] == 0)
                {
                    availableMoves.Add(i * 3 + j);
                }
            }
        }

        if (availableMoves.Count > 0)
        {
            return availableMoves[Random.Range(0, availableMoves.Count)];
        }
        return -1; // ���� ��������� ����
    }

    /// <summary>
    /// ����������� �������� ̳����� ��� ������ ����.
    /// </summary>
    /// <param name="isMaximizing">�� �� ��� ������, �� �������� (ز)?</param>
    private int Minimax(int[,] board, int depth, bool isMaximizing, int aiMark, int playerMark)
    {
        int score = EvaluateBoard(board);

        // ����� ���������� ������
        if (score == aiMark) return 10 - depth; // ز ������
        if (score == playerMark) return -10 + depth; // ������� ������
        if (!IsMovesLeft(board)) return 0; // ͳ���

        if (isMaximizing)
        {
            int best = int.MinValue;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (board[i, j] == 0)
                    {
                        board[i, j] = aiMark;
                        best = Mathf.Max(best, Minimax(board, depth + 1, !isMaximizing, aiMark, playerMark));
                        board[i, j] = 0;
                    }
                }
            }
            return best;
        }
        else // ճ� ������, �� �����
        {
            int best = int.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (board[i, j] == 0)
                    {
                        board[i, j] = playerMark;
                        best = Mathf.Min(best, Minimax(board, depth + 1, !isMaximizing, aiMark, playerMark));
                        board[i, j] = 0;
                    }
                }
            }
            return best;
        }
    }

    /// <summary>
    /// ������ �������� ���� �����, ��� ���������, �� � ����������.
    /// </summary>
    /// <returns>Գ��� ��������� (1 ��� 2) ��� 0, ���� ��������� ����.</returns>
    public static int EvaluateBoard(int[,] b)
    {
        // �������� �����
        for (int row = 0; row < 3; row++)
        {
            if (b[row, 0] == b[row, 1] && b[row, 1] == b[row, 2] && b[row, 0] != 0)
                return b[row, 0];
        }
        // �������� ��������
        for (int col = 0; col < 3; col++)
        {
            if (b[0, col] == b[1, col] && b[1, col] == b[2, col] && b[0, col] != 0)
                return b[0, col];
        }
        // �������� ���������
        if (b[0, 0] == b[1, 1] && b[1, 1] == b[2, 2] && b[0, 0] != 0)
            return b[0, 0];
        if (b[0, 2] == b[1, 1] && b[1, 1] == b[2, 0] && b[0, 2] != 0)
            return b[0, 2];

        return 0; // ���� ���������
    }

    /// <summary>
    /// ��������, �� ���������� ���� ������� �� �����.
    /// </summary>
    private bool IsMovesLeft(int[,] board)
    {
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                if (board[i, j] == 0)
                    return true;
        return false;
    }
}
