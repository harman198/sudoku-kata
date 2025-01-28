namespace SudokuKata
{
    public class SudokuBoardAndGameStack
    {
        private const string LINE = "+---+---+---+";
        private const string MIDDLE = "|...|...|...|";

        // Prepare empty board
        public char[][] Board { get; } = [LINE.ToCharArray(),
                                            MIDDLE.ToCharArray(),
                                            MIDDLE.ToCharArray(),
                                            MIDDLE.ToCharArray(),
                                            LINE.ToCharArray(),
                                            MIDDLE.ToCharArray(),
                                            MIDDLE.ToCharArray(),
                                            MIDDLE.ToCharArray(),
                                            LINE.ToCharArray(),
                                            MIDDLE.ToCharArray(),
                                            MIDDLE.ToCharArray(),
                                            MIDDLE.ToCharArray(),
                                            LINE.ToCharArray()
                                        ];
        public Stack<int[]> StateStack { get; } = new();
    }
}
