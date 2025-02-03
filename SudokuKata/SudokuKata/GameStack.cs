using System.Text;

namespace SudokuKata
{
    public class GameStack
    {
        private const string LINE = "+---+---+---+";
        private const string MIDDLE = "|...|...|...|";

        // Prepare empty board
        private readonly char[][] Board = [LINE.ToCharArray(),
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

        public string PrintBoard
        {
            get
            {
                if (StateStack.Count == 0)
                {
                    return string.Join(Environment.NewLine, Board.Select(s => new string(s)).ToArray());
                }
                else
                {
                    var stringBuilder = new StringBuilder();
                    var row = 0;
                    foreach (var item in StateStack.Peek().Select(x => x == 0 ? "." : x.ToString()).Chunk(3).ToList())
                    {
                        if (row % 9 == 0)
                            stringBuilder.AppendLine(LINE);
                        if (row % 3 == 0)
                            stringBuilder.Append("|");
                        stringBuilder.Append(string.Join("", item)).Append("|");
                        if (row % 3 == 2)
                            stringBuilder.AppendLine();

                        row++;
                    }
                    stringBuilder.Append(LINE);
                    return stringBuilder.ToString().TrimEnd();
                }
            }
        }
    }
}
