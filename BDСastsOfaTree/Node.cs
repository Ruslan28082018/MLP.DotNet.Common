using System;
using System.Collections.Generic;
using System.Text;

namespace BDСastsOfaTree
{
    public class Node
    {
        public string Data;
        public Node Parent;
        public List<Node> Children = new List<Node>();
    }
}
