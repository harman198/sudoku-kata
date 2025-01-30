namespace SudokuKata
{
    internal class SudokuGameCreator(Random random)
    {
        private readonly Random _random = random;

        public SudokuBoardAndGameStack CreateSolvedBoard()
        {
            var sudokuBoardAndGameStack = new SudokuBoardAndGameStack();
            Stack<int> rowIndexStack = new();
            Stack<int> colIndexStack = new();
            Stack<bool[]> usedDigitsStack = new();
            Stack<int> lastDigitStack = new();

            // Indicates operation to perform next
            // - expand - finds next empty cell and puts new state on stacks
            // - move - finds next candidate number at current pos and applies it to current state
            // - collapse - pops current state from stack as it did not yield a solution
            string command = "expand";
            while (sudokuBoardAndGameStack.StateStack.Count <= 9 * 9)
            {
                if (command == "expand")
                    command = ExpandBoard(sudokuBoardAndGameStack, rowIndexStack, colIndexStack, usedDigitsStack, lastDigitStack); // if (command == "expand")
                else if (command == "collapse")
                {
                    command = CollapseBoard(sudokuBoardAndGameStack, rowIndexStack, colIndexStack, usedDigitsStack, lastDigitStack);
                }
                else if (command == "move")
                {

                    int rowToMove = rowIndexStack.Peek();
                    int colToMove = colIndexStack.Peek();
                    int digitToMove = lastDigitStack.Pop();

                    int rowToWrite = rowToMove + rowToMove / 3 + 1;
                    int colToWrite = colToMove + colToMove / 3 + 1;

                    bool[] usedDigits = usedDigitsStack.Peek();
                    int[] currentState = sudokuBoardAndGameStack.StateStack.Peek();
                    int currentStateIndex = 9 * rowToMove + colToMove;

                    int movedToDigit = digitToMove + 1;
                    while (movedToDigit <= 9 && usedDigits[movedToDigit - 1])
                        movedToDigit += 1;

                    if (digitToMove > 0)
                    {
                        usedDigits[digitToMove - 1] = false;
                        currentState[currentStateIndex] = 0;
                        sudokuBoardAndGameStack.Board[rowToWrite][colToWrite] = '.';
                    }

                    if (movedToDigit <= 9)
                    {
                        lastDigitStack.Push(movedToDigit);
                        usedDigits[movedToDigit - 1] = true;
                        currentState[currentStateIndex] = movedToDigit;
                        sudokuBoardAndGameStack.Board[rowToWrite][colToWrite] = (char)('0' + movedToDigit);

                        // Next possible digit was found at current position
                        // Next step will be to expand the state
                        command = "expand";
                    }
                    else
                    {
                        // No viable candidate was found at current position - pop it in the next iteration
                        lastDigitStack.Push(0);
                        command = "collapse";
                    }
                } // if (command == "move")

            }

            return sudokuBoardAndGameStack;
        }

        private static string CollapseBoard(SudokuBoardAndGameStack sudokuBoardAndGameStack, Stack<int> rowIndexStack, Stack<int> colIndexStack, Stack<bool[]> usedDigitsStack, Stack<int> lastDigitStack)
        {
            sudokuBoardAndGameStack.StateStack.Pop();
            rowIndexStack.Pop();
            colIndexStack.Pop();
            usedDigitsStack.Pop();
            lastDigitStack.Pop();

            return "move";
        }

        private string ExpandBoard(SudokuBoardAndGameStack sudokuBoardAndGameStack, Stack<int> rowIndexStack, Stack<int> colIndexStack, Stack<bool[]> usedDigitsStack, Stack<int> lastDigitStack)
        {
            int[] currentState = new int[9 * 9];

            if (sudokuBoardAndGameStack.StateStack.Count > 0)
            {
                Array.Copy(sudokuBoardAndGameStack.StateStack.Peek(), currentState, currentState.Length);
            }

            int bestRow = -1;
            int bestCol = -1;
            bool[] bestUsedDigits = [];
            int bestCandidatesCount = -1;
            int bestRandomValue = -1;
            bool containsUnsolvableCells = false;

            for (int index = 0; index < currentState.Length; index++)
                if (currentState[index] == 0)
                {

                    int row = index / 9;
                    int col = index % 9;
                    int blockRow = row / 3;
                    int blockCol = col / 3;

                    bool[] isDigitUsed = new bool[9];

                    for (int i = 0; i < 9; i++)
                    {
                        int rowDigit = currentState[9 * i + col];
                        if (rowDigit > 0)
                            isDigitUsed[rowDigit - 1] = true;

                        int colDigit = currentState[9 * row + i];
                        if (colDigit > 0)
                            isDigitUsed[colDigit - 1] = true;

                        int blockDigit = currentState[(blockRow * 3 + i / 3) * 9 + (blockCol * 3 + i % 3)];
                        if (blockDigit > 0)
                            isDigitUsed[blockDigit - 1] = true;
                    } // for (i = 0..8)

                    int candidatesCount = isDigitUsed.Where(used => !used).Count();

                    if (candidatesCount == 0)
                    {
                        containsUnsolvableCells = true;
                        break;
                    }

                    int randomValue = _random.Next();

                    if (bestCandidatesCount < 0 ||
                        candidatesCount < bestCandidatesCount ||
                        (candidatesCount == bestCandidatesCount && randomValue < bestRandomValue))
                    {
                        bestRow = row;
                        bestCol = col;
                        bestUsedDigits = isDigitUsed;
                        bestCandidatesCount = candidatesCount;
                        bestRandomValue = randomValue;
                    }

                } // for (index = 0..81)

            if (!containsUnsolvableCells)
            {
                sudokuBoardAndGameStack.StateStack.Push(currentState);
                rowIndexStack.Push(bestRow);
                colIndexStack.Push(bestCol);
                usedDigitsStack.Push(bestUsedDigits);
                lastDigitStack.Push(0); // No digit was tried at this position
            }

            // Always try to move after expand
            return "move";
        }
    }
}