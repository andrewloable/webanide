using System;
using Webanide;
using Webanide.Models;

namespace Sample1
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var ui = UI.New("", "", 800, 800);
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
    }
}
