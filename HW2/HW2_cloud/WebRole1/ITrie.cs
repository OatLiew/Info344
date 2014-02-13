using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    public interface ITrie
    {
        void Add(string str);
        List<string> SearchPrefix(string str, int top = -1);
    }
}