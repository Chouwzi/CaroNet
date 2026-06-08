namespace CaroNet.Shared.Game;

public static class CaroRuleEngine
{
    private static readonly (int dRow, int dCol)[] Directions =
    {
        (0, 1),  
        (1, 0),  
        (1, 1),   
        (1, -1)  
    };

    public static bool IsValidPosition(int size, int r, int c)
    {
        return r >= 0 && r < size && c >= 0 && c < size;
    }

    public static bool CheckWin(CaroGameState state, int lastRow, int lastCol)
    {
        CellState targetState = state[lastRow, lastCol];
        if (targetState == CellState.Empty) return false;

        int size = state.Size;

        foreach (var (dRow, dCol) in Directions)
        {
            int totalConsecutive = 1;

            int rPos = lastRow + dRow;
            int cPos = lastCol + dCol;
            while (IsValidPosition(size, rPos, cPos) && state[rPos, cPos] == targetState)
            {
                totalConsecutive++;
                rPos += dRow;
                cPos += dCol;
            }

            int rNeg = lastRow - dRow;
            int cNeg = lastCol - dCol;
            while (IsValidPosition(size, rNeg, cNeg) && state[rNeg, cNeg] == targetState)
            {
                totalConsecutive++;
                rNeg -= dRow;
                cNeg -= dCol;
            }

            if (totalConsecutive == 5)
            {
                return true;
            }
        }
        return false;
    }
}