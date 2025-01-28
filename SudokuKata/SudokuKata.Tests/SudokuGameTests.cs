namespace SudokuKata.Tests
{
    public class SudokuGameTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public Task Verify_ExistingFunctionality_Tests(int seed)
        {
            using StringWriter streamWriter = new();

            Console.SetOut(streamWriter);
            SudokuGame.Play(new Random(seed));

            return Verify(streamWriter.ToString())
                    .UseParameters(seed);
        }
    }
}