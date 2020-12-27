using System;

namespace Program
{
    class Program
    {
        static void Main(string[] args)
        {
            var scrapper = new JVCScrapper(2103);

            scrapper.GetGrades();
        }
    }
}
