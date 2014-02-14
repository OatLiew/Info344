using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
   
   public class Node
   {
            public char NodeKey { get; set; }
            public int NoOfPrefix { get; set; }
            public Dictionary<int, Node> Children { get; set; }
            public bool IsWord { get; set; }
   }
    
}