using System;
using System.Collections.Generic;

namespace LocalizationManager
{
    class CustomComparer : IComparer<string>
    {
        public int Compare(string s1, string s2)
        {
            int n1 = -1;
            int n2 = -1;

            bool isParseIntS1 = Int32.TryParse(s1, out n1) == true;
            bool isParseIntS2 = Int32.TryParse(s2, out n2) == true;

            if (isParseIntS1 == true && isParseIntS2 == true)
            {
                return n1 - n2;
            }
            else
            {
                if (isParseIntS1 == true && isParseIntS2 == false)
                {
                    return -1;
                }
                else if (isParseIntS1 == false && isParseIntS2 == true)
                {
                    return 1;
                }
            }

            return string.Compare(s1, s2);
        }
    }
}
