﻿using System.Text;

namespace SudokuKata;

public static class SudokuGame
{

    internal static void Play()
    {
        Random rng = new();
        Play(rng);
    }

    public static void Play(Random rng)
    {
        #region Construct fully populated board

        // Construct board to be solved
        // Top element is current state of the board
        GameStack gameStack = new SudokuGameCreator(rng).CreateSolvedBoard();

        Console.WriteLine();
        Console.WriteLine("Final look of the solved board:");
        Console.WriteLine(gameStack.PrintBoard);
        #endregion

        #region Generate inital board from the completely solved one
        // Board is solved at this point.
        // Now pick subset of digits as the starting position.
        int remainingDigits = 30;
        int maxRemovedPerBlock = 6;
        int[,] removedPerBlock = new int[3, 3];
        int[] positions = Enumerable.Range(0, 9 * 9).ToArray();
        int[] state = gameStack.StateStack.Peek();

        int[] finalState = new int[state.Length];
        Array.Copy(state, finalState, finalState.Length);

        int removedPos = 0;
        while (removedPos < 9 * 9 - remainingDigits)
        {
            int curRemainingDigits = positions.Length - removedPos;
            int indexToPick = removedPos + rng.Next(curRemainingDigits);

            int row = positions[indexToPick] / 9;
            int col = positions[indexToPick] % 9;

            int blockRowToRemove = row / 3;
            int blockColToRemove = col / 3;

            if (removedPerBlock[blockRowToRemove, blockColToRemove] >= maxRemovedPerBlock)
                continue;

            removedPerBlock[blockRowToRemove, blockColToRemove] += 1;

            (positions[indexToPick], positions[removedPos]) = (positions[removedPos], positions[indexToPick]);
            int rowToWrite = row + row / 3 + 1;
            int colToWrite = col + col / 3 + 1;

            int stateIndex = 9 * row + col;
            state[stateIndex] = 0;

            removedPos += 1;
        }

        Console.WriteLine();
        Console.WriteLine("Starting look of the board to solve:");
        Console.WriteLine(gameStack.PrintBoard);
        #endregion

        #region Prepare lookup structures that will be used in further execution
        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine();

        Dictionary<int, int> maskToOnesCount = new()
        {
            [0] = 0
        };
        for (int i = 1; i < (1 << 9); i++)
        {
            int smaller = i >> 1;
            int increment = i & 1;
            maskToOnesCount[i] = maskToOnesCount[smaller] + increment;
        }

        Dictionary<int, int> singleBitToIndex = [];
        for (int i = 0; i < 9; i++)
            singleBitToIndex[1 << i] = i;

        int allOnes = (1 << 9) - 1;
        #endregion

        bool changeMade = true;
        while (changeMade)
        {
            changeMade = false;

            #region Calculate candidates for current state of the board
            int[] candidateMasks = new int[state.Length];

            for (int i = 0; i < state.Length; i++)
                if (state[i] == 0)
                {

                    int row = i / 9;
                    int col = i % 9;
                    int blockRow = row / 3;
                    int blockCol = col / 3;

                    int colidingNumbers = 0;
                    for (int j = 0; j < 9; j++)
                    {
                        int rowSiblingIndex = 9 * row + j;
                        int colSiblingIndex = 9 * j + col;
                        int blockSiblingIndex = 9 * (blockRow * 3 + j / 3) + blockCol * 3 + j % 3;

                        int rowSiblingMask = 1 << (state[rowSiblingIndex] - 1);
                        int colSiblingMask = 1 << (state[colSiblingIndex] - 1);
                        int blockSiblingMask = 1 << (state[blockSiblingIndex] - 1);

                        colidingNumbers = colidingNumbers | rowSiblingMask | colSiblingMask | blockSiblingMask;
                    }

                    candidateMasks[i] = allOnes & ~colidingNumbers;
                }
            #endregion

            #region Build a collection (named cellGroups) which maps cell indices into distinct groups (rows/columns/blocks)
            var rowsIndices = state
                .Select((value, index) => new IndexClass(index / 9, $"row #{index / 9 + 1}", index, index / 9, index % 9))
                .GroupBy(tuple => tuple.Discriminator);

            var columnIndices = state
                .Select((value, index) => new IndexClass(9 + index % 9, $"column #{index % 9 + 1}", index, index / 9, index % 9))
                .GroupBy(tuple => tuple.Discriminator);

            var blockIndices = state
                .Select((value, index) => new
                {
                    Row = index / 9,
                    Column = index % 9,
                    Index = index
                })
                .Select(tuple => new IndexClass(18 + 3 * (tuple.Row / 3) + tuple.Column / 3, $"block ({tuple.Row / 3 + 1}, {tuple.Column / 3 + 1})", tuple.Index, tuple.Row, tuple.Column))
                .GroupBy(tuple => tuple.Discriminator);

            var cellGroups = rowsIndices.Concat(columnIndices).Concat(blockIndices).ToList();
            #endregion

            bool stepChangeMade = true;
            while (stepChangeMade)
            {
                stepChangeMade = false;

                #region Pick cells with only one candidate left

                int[] singleCandidateIndices =
                    candidateMasks
                        .Select((mask, index) => new
                        {
                            CandidatesCount = maskToOnesCount[mask],
                            Index = index
                        })
                        .Where(tuple => tuple.CandidatesCount == 1)
                        .Select(tuple => tuple.Index)
                        .ToArray();

                if (singleCandidateIndices.Length > 0)
                {
                    int pickSingleCandidateIndex = rng.Next(singleCandidateIndices.Length);
                    int singleCandidateIndex = singleCandidateIndices[pickSingleCandidateIndex];
                    int candidateMask = candidateMasks[singleCandidateIndex];
                    int candidate = singleBitToIndex[candidateMask];

                    int row = singleCandidateIndex / 9;
                    int col = singleCandidateIndex % 9;

                    int rowToWrite = row + row / 3 + 1;
                    int colToWrite = col + col / 3 + 1;

                    state[singleCandidateIndex] = candidate + 1;
                    candidateMasks[singleCandidateIndex] = 0;
                    changeMade = true;

                    Console.WriteLine("({0}, {1}) can only contain {2}.", row + 1, col + 1, candidate + 1);
                }

                #endregion

                #region Try to find a number which can only appear in one place in a row/column/block

                if (!changeMade)
                {
                    List<string> groupDescriptions = [];
                    List<int> candidateRowIndices = [];
                    List<int> candidateColIndices = [];
                    List<int> candidates = [];

                    for (int digit = 1; digit <= 9; digit++)
                    {
                        int mask = 1 << (digit - 1);
                        for (int cellGroup = 0; cellGroup < 9; cellGroup++)
                        {
                            int rowNumberCount = 0;
                            int indexInRow = 0;

                            int colNumberCount = 0;
                            int indexInCol = 0;

                            int blockNumberCount = 0;
                            int indexInBlock = 0;

                            for (int indexInGroup = 0; indexInGroup < 9; indexInGroup++)
                            {
                                int rowStateIndex = 9 * cellGroup + indexInGroup;
                                int colStateIndex = 9 * indexInGroup + cellGroup;
                                int blockRowIndex = (cellGroup / 3) * 3 + indexInGroup / 3;
                                int blockColIndex = (cellGroup % 3) * 3 + indexInGroup % 3;
                                int blockStateIndex = blockRowIndex * 9 + blockColIndex;

                                if ((candidateMasks[rowStateIndex] & mask) != 0)
                                {
                                    rowNumberCount += 1;
                                    indexInRow = indexInGroup;
                                }

                                if ((candidateMasks[colStateIndex] & mask) != 0)
                                {
                                    colNumberCount += 1;
                                    indexInCol = indexInGroup;
                                }

                                if ((candidateMasks[blockStateIndex] & mask) != 0)
                                {
                                    blockNumberCount += 1;
                                    indexInBlock = indexInGroup;
                                }
                            }

                            if (rowNumberCount == 1)
                            {
                                groupDescriptions.Add($"Row #{cellGroup + 1}");
                                candidateRowIndices.Add(cellGroup);
                                candidateColIndices.Add(indexInRow);
                                candidates.Add(digit);
                            }

                            if (colNumberCount == 1)
                            {
                                groupDescriptions.Add($"Column #{cellGroup + 1}");
                                candidateRowIndices.Add(indexInCol);
                                candidateColIndices.Add(cellGroup);
                                candidates.Add(digit);
                            }

                            if (blockNumberCount == 1)
                            {
                                int blockRow = cellGroup / 3;
                                int blockCol = cellGroup % 3;

                                groupDescriptions.Add($"Block ({blockRow + 1}, {blockCol + 1})");
                                candidateRowIndices.Add(blockRow * 3 + indexInBlock / 3);
                                candidateColIndices.Add(blockCol * 3 + indexInBlock % 3);
                                candidates.Add(digit);
                            }
                        } // for (cellGroup = 0..8)
                    } // for (digit = 1..9)

                    if (candidates.Count > 0)
                    {
                        int index = rng.Next(candidates.Count);
                        string description = groupDescriptions.ElementAt(index);
                        int row = candidateRowIndices.ElementAt(index);
                        int col = candidateColIndices.ElementAt(index);
                        int digit = candidates.ElementAt(index);
                        int rowToWrite = row + row / 3 + 1;
                        int colToWrite = col + col / 3 + 1;

                        string message = $"{description} can contain {digit} only at ({row + 1}, {col + 1}).";

                        int stateIndex = 9 * row + col;
                        state[stateIndex] = digit;
                        candidateMasks[stateIndex] = 0;

                        changeMade = true;

                        Console.WriteLine(message);
                    }
                }

                #endregion

                #region Try to find pairs of digits in the same row/column/block and remove them from other colliding cells
                if (!changeMade)
                {
                    IEnumerable<int> twoDigitMasks =
                        candidateMasks.Where(mask => maskToOnesCount[mask] == 2).Distinct().ToList();

                    var groups =
                        twoDigitMasks
                            .SelectMany(mask =>
                                cellGroups
                                    .Where(group => group.Count(tuple => candidateMasks[tuple.Index] == mask) == 2)
                                    .Where(group => group.Any(tuple => candidateMasks[tuple.Index] != mask && (candidateMasks[tuple.Index] & mask) > 0))
                                    .Select(group => new
                                    {
                                        Mask = mask,
                                        Discriminator = group.Key,
                                        group.First().Description,
                                        Cells = group
                                    }))
                            .ToList();

                    if (groups.Count != 0)
                    {
                        foreach (var group in groups)
                        {
                            var cells =
                                group.Cells
                                    .Where(
                                        cell =>
                                            candidateMasks[cell.Index] != group.Mask &&
                                            (candidateMasks[cell.Index] & group.Mask) > 0)
                                    .ToList();

                            var maskCells =
                                group.Cells
                                    .Where(cell => candidateMasks[cell.Index] == group.Mask)
                                    .ToArray();


                            if (cells.Count != 0)
                            {
                                int upper = 0;
                                int lower = 0;
                                int temp = group.Mask;

                                int value = 1;
                                while (temp > 0)
                                {
                                    if ((temp & 1) > 0)
                                    {
                                        lower = upper;
                                        upper = value;
                                    }
                                    temp >>= 1;
                                    value += 1;
                                }

                                Console.WriteLine(
                                    $"Values {lower} and {upper} in {group.Description} are in cells ({maskCells[0].Row + 1}, {maskCells[0].Column + 1}) and ({maskCells[1].Row + 1}, {maskCells[1].Column + 1}).");

                                foreach (var cell in cells)
                                {
                                    int maskToRemove = candidateMasks[cell.Index] & group.Mask;
                                    List<int> valuesToRemove = [];
                                    int curValue = 1;
                                    while (maskToRemove > 0)
                                    {
                                        if ((maskToRemove & 1) > 0)
                                        {
                                            valuesToRemove.Add(curValue);
                                        }
                                        maskToRemove >>= 1;
                                        curValue += 1;
                                    }

                                    string valuesReport = string.Join(", ", valuesToRemove.ToArray());
                                    Console.WriteLine($"{valuesReport} cannot appear in ({cell.Row + 1}, {cell.Column + 1}).");

                                    candidateMasks[cell.Index] &= ~group.Mask;
                                    stepChangeMade = true;
                                }
                            }
                        }
                    }

                }
                #endregion

                #region Try to find groups of digits of size N which only appear in N cells within row/column/block
                // When a set of N digits only appears in N cells within row/column/block, then no other digit can appear in the same set of cells
                // All other candidates can then be removed from those cells

                if (!changeMade && !stepChangeMade)
                {
                    IEnumerable<int> masks =
                        maskToOnesCount
                            .Where(tuple => tuple.Value > 1)
                            .Select(tuple => tuple.Key).ToList();

                    var groupsWithNMasks =
                        masks
                            .SelectMany(mask =>
                                cellGroups
                                    .Where(group => group.All(cell => state[cell.Index] == 0 || (mask & (1 << (state[cell.Index] - 1))) == 0))
                                    .Select(group => new
                                    {
                                        Mask = mask,
                                        group.First().Description,
                                        Cells = group,
                                        CellsWithMask =
                                            group.Where(cell => state[cell.Index] == 0 && (candidateMasks[cell.Index] & mask) != 0).ToList(),
                                        CleanableCellsCount =
                                            group.Count(
                                                cell => state[cell.Index] == 0 &&
                                                    (candidateMasks[cell.Index] & mask) != 0 &&
                                                    (candidateMasks[cell.Index] & ~mask) != 0)
                                    }))
                            .Where(group => group.CellsWithMask.Count == maskToOnesCount[group.Mask])
                            .ToList();

                    foreach (var groupWithNMasks in groupsWithNMasks)
                    {
                        int mask = groupWithNMasks.Mask;

                        if (groupWithNMasks.Cells
                            .Any(cell =>
                                (candidateMasks[cell.Index] & mask) != 0 &&
                                (candidateMasks[cell.Index] & ~mask) != 0))
                        {
                            StringBuilder message = new();
                            message.Append($"In {groupWithNMasks.Description} values ");

                            string separator = string.Empty;
                            int temp = mask;
                            int curValue = 1;
                            while (temp > 0)
                            {
                                if ((temp & 1) > 0)
                                {
                                    message.Append($"{separator}{curValue}");
                                    separator = ", ";
                                }
                                temp >>= 1;
                                curValue += 1;
                            }

                            message.Append(" appear only in cells");
                            foreach (var cell in groupWithNMasks.CellsWithMask)
                            {
                                message.Append($" ({cell.Row + 1}, {cell.Column + 1})");
                            }

                            message.Append(" and other values cannot appear in those cells.");

                            Console.WriteLine(message.ToString());
                        }

                        foreach (var cell in groupWithNMasks.CellsWithMask)
                        {
                            int maskToClear = candidateMasks[cell.Index] & ~groupWithNMasks.Mask;
                            if (maskToClear == 0)
                                continue;

                            candidateMasks[cell.Index] &= groupWithNMasks.Mask;
                            stepChangeMade = true;

                            int valueToClear = 1;

                            string separator = string.Empty;
                            StringBuilder message = new();

                            while (maskToClear > 0)
                            {
                                if ((maskToClear & 1) > 0)
                                {
                                    message.Append($"{separator}{valueToClear}");
                                    separator = ", ";
                                }
                                maskToClear >>= 1;
                                valueToClear += 1;
                            }

                            message.Append($" cannot appear in cell ({cell.Row + 1}, {cell.Column + 1}).");
                            Console.WriteLine(message.ToString());

                        }
                    }
                }

                #endregion
            }

            #region Final attempt - look if the board has multiple solutions
            if (!changeMade)
            {
                // This is the last chance to do something in this iteration:
                // If this attempt fails, board will not be entirely solved.

                // Try to see if there are pairs of values that can be exchanged arbitrarily
                // This happens when board has more than one valid solution

                Queue<int> candidateIndex1 = new();
                Queue<int> candidateIndex2 = new();
                Queue<int> candidateDigit1 = new();
                Queue<int> candidateDigit2 = new();

                for (int i = 0; i < candidateMasks.Length - 1; i++)
                {
                    if (maskToOnesCount[candidateMasks[i]] == 2)
                    {
                        int row = i / 9;
                        int col = i % 9;
                        int blockIndex = 3 * (row / 3) + col / 3;

                        int temp = candidateMasks[i];
                        int lower = 0;
                        int upper = 0;
                        for (int digit = 1; temp > 0; digit++)
                        {
                            if ((temp & 1) != 0)
                            {
                                lower = upper;
                                upper = digit;
                            }
                            temp >>= 1;
                        }

                        for (int j = i + 1; j < candidateMasks.Length; j++)
                        {
                            if (candidateMasks[j] == candidateMasks[i])
                            {
                                int row1 = j / 9;
                                int col1 = j % 9;
                                int blockIndex1 = 3 * (row1 / 3) + col1 / 3;

                                if (row == row1 || col == col1 || blockIndex == blockIndex1)
                                {
                                    candidateIndex1.Enqueue(i);
                                    candidateIndex2.Enqueue(j);
                                    candidateDigit1.Enqueue(lower);
                                    candidateDigit2.Enqueue(upper);
                                }
                            }
                        }
                    }
                }

                // At this point we have the lists with pairs of cells that might pick one of two digits each
                // Now we have to check whether that is really true - does the board have two solutions?

                List<int> stateIndex1 = [];
                List<int> stateIndex2 = [];
                List<int> value1 = [];
                List<int> value2 = [];

                while (candidateIndex1.Count != 0)
                {
                    int index1 = candidateIndex1.Dequeue();
                    int index2 = candidateIndex2.Dequeue();
                    int digit1 = candidateDigit1.Dequeue();
                    int digit2 = candidateDigit2.Dequeue();

                    int[] alternateState = new int[finalState.Length];
                    Array.Copy(state, alternateState, alternateState.Length);

                    if (finalState[index1] == digit1)
                    {
                        alternateState[index1] = digit2;
                        alternateState[index2] = digit1;
                    }
                    else
                    {
                        alternateState[index1] = digit1;
                        alternateState[index2] = digit2;
                    }

                    // What follows below is a complete copy-paste of the solver which appears at the beginning of this method
                    // However, the algorithm couldn't be applied directly and it had to be modified.
                    // Implementation below assumes that the board might not have a solution.
                    var stateStack = new Stack<int[]>();

                    // Top elements are (row, col) of cell which has been modified compared to previous state
                    Stack<int> rowIndexStack = new();
                    Stack<int> colIndexStack = new();

                    // Top element indicates candidate digits (those with False) for (row, col)
                    Stack<bool[]> usedDigitsStack = new();

                    // Top element is the value that was set on (row, col)
                    Stack<int> lastDigitStack = new();

                    string command = "expand";
                    while (command != "complete" && command != "fail")
                    {
                        if (command == "expand")
                        {
                            int[] currentState = new int[9 * 9];

                            if (stateStack.Count != 0)
                            {
                                Array.Copy(stateStack.Peek(), currentState, currentState.Length);
                            }
                            else
                            {
                                Array.Copy(alternateState, currentState, currentState.Length);
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

                                    int randomValue = rng.Next();

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
                                stateStack.Push(currentState);
                                rowIndexStack.Push(bestRow);
                                colIndexStack.Push(bestCol);
                                usedDigitsStack.Push(bestUsedDigits);
                                lastDigitStack.Push(0); // No digit was tried at this position
                            }

                            // Always try to move after expand
                            command = "move";

                        } // if (command == "expand")
                        else if (command == "collapse")
                        {
                            stateStack.Pop();
                            rowIndexStack.Pop();
                            colIndexStack.Pop();
                            usedDigitsStack.Pop();
                            lastDigitStack.Pop();

                            if (stateStack.Count != 0)
                                command = "move"; // Always try to move after collapse
                            else
                                command = "fail";
                        }
                        else if (command == "move")
                        {

                            int rowToMove = rowIndexStack.Peek();
                            int colToMove = colIndexStack.Peek();
                            int digitToMove = lastDigitStack.Pop();

                            int rowToWrite = rowToMove + rowToMove / 3 + 1;
                            int colToWrite = colToMove + colToMove / 3 + 1;

                            bool[] usedDigits = usedDigitsStack.Peek();
                            int[] currentState = stateStack.Peek();
                            int currentStateIndex = 9 * rowToMove + colToMove;

                            int movedToDigit = digitToMove + 1;
                            while (movedToDigit <= 9 && usedDigits[movedToDigit - 1])
                                movedToDigit += 1;

                            if (digitToMove > 0)
                            {
                                usedDigits[digitToMove - 1] = false;
                                currentState[currentStateIndex] = 0;
                            }

                            if (movedToDigit <= 9)
                            {
                                lastDigitStack.Push(movedToDigit);
                                usedDigits[movedToDigit - 1] = true;
                                currentState[currentStateIndex] = movedToDigit;

                                if (currentState.Any(digit => digit == 0))
                                    command = "expand";
                                else
                                    command = "complete";
                            }
                            else
                            {
                                // No viable candidate was found at current position - pop it in the next iteration
                                lastDigitStack.Push(0);
                                command = "collapse";
                            }
                        } // if (command == "move")

                    } // while (command != "complete" && command != "fail")

                    if (command == "complete")
                    {   // Board was solved successfully even with two digits swapped
                        stateIndex1.Add(index1);
                        stateIndex2.Add(index2);
                        value1.Add(digit1);
                        value2.Add(digit2);
                    }
                } // while (candidateIndex1.Any())

                if (stateIndex1.Count != 0)
                {
                    int pos = rng.Next(stateIndex1.Count);
                    int index1 = stateIndex1.ElementAt(pos);
                    int index2 = stateIndex2.ElementAt(pos);
                    int digit1 = value1.ElementAt(pos);
                    int digit2 = value2.ElementAt(pos);
                    int row1 = index1 / 9;
                    int col1 = index1 % 9;
                    int row2 = index2 / 9;
                    int col2 = index2 % 9;

                    string description = string.Empty;

                    if (index1 / 9 == index2 / 9)
                    {
                        description = $"row #{index1 / 9 + 1}";
                    }
                    else if (index1 % 9 == index2 % 9)
                    {
                        description = $"column #{index1 % 9 + 1}";
                    }
                    else
                    {
                        description = $"block ({row1 / 3 + 1}, {col1 / 3 + 1})";
                    }

                    state[index1] = finalState[index1];
                    state[index2] = finalState[index2];
                    candidateMasks[index1] = 0;
                    candidateMasks[index2] = 0;
                    changeMade = true;

                    for (int i = 0; i < state.Length; i++)
                    {
                        int tempRow = i / 9;
                        int tempCol = i % 9;
                        int rowToWrite = tempRow + tempRow / 3 + 1;
                        int colToWrite = tempCol + tempCol / 3 + 1;
                    }

                    Console.WriteLine($"Guessing that {digit1} and {digit2} are arbitrary in {description} (multiple solutions): Pick {finalState[index1]}->({row1 + 1}, {col1 + 1}), {finalState[index2]}->({row2 + 1}, {col2 + 1}).");
                }
            }
            #endregion

            if (changeMade)
            {
                #region Print the board as it looks after one change was made to it
                Console.WriteLine(gameStack.PrintBoard);
                string code =
                    gameStack.PrintBoard
                        .Replace("-", string.Empty)
                        .Replace(Environment.NewLine, string.Empty)
                        .Replace("+", string.Empty)
                        .Replace("|", string.Empty)
                        .Replace(".", "0");

                Console.WriteLine("Code: {0}", code);
                Console.WriteLine();
                #endregion
            }
        }
    }
}

internal record IndexClass(int Discriminator, string Description, int Index, int Row, int Column);
