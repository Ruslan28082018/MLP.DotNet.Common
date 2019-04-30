using System;
using System.Collections.Generic;
using System.Text;

namespace MLP.Tools
{
    public class Node<TData>
    {
        public TData Data;
        public Node<TData> Parent;
        public List<Node<TData>> Children = new List<Node<TData>>();

        public void AddChild(TData aData)
            => Children.Add(new Node<TData>()
                                            {
                                                Data = aData,
                                                Parent = this,
                                            });
    }
}
