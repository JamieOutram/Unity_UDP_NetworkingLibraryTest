using System;
using System.Linq;

namespace UnityNetworkingLibraryTest.Utils
{
    class RandomExtensions
    {
        //public static Random random = new Random();
        public static string RandomString(int length, Random rand)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[rand.Next(s.Length)]).ToArray());
        }

    }
}
