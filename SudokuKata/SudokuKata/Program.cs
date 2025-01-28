using System;

namespace SudokuKata
{
    class Program
    {
        static void Main(string[] args)
        {
            SudokuGame.Play();

            Console.WriteLine();
            Console.Write("Press ENTER to exit... ");
            Console.ReadLine();
        }
    }
}