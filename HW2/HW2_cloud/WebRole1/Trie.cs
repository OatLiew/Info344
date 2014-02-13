using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace WebRole1
{
    public class Trie : ITrie
    {
        private int _maxSearchCount = 10;

        private Node _root;

        public Trie()
        {
            _root = new Node() { NodeKey = ' ' };
        }

        public Trie(int maxSearchCount)
        {
            _root = new Node() { NodeKey = ' ' };
            _maxSearchCount = maxSearchCount;
        }

        public void Add(string str)
        {
            Node cur = _root;
            Node tmp = null;

            foreach (char ch in str)
            {
                if (cur.Children == null)
                    cur.Children = new Dictionary<int, Node>();

                if (!cur.Children.Keys.Contains(ch))
                {
                    tmp = new Node() { NodeKey = ch };
                    cur.Children.Add(ch, tmp);
                }

                cur = cur.Children[ch];
                cur.NoOfPrefix += 1;
            }
            cur.IsWord = true;
        }

        public List<string> SearchPrefix(string str, int top = -1)
        {
            List<string> result = new List<string>();
            Node cur = _root;
            string prefix = String.Empty;
            bool fail = false;

            foreach (char ch in str)
            {
                if (cur.Children == null)
                {
                    fail = true;
                    break;
                }

                if (cur.Children.Keys.Contains(ch))
                {
                    prefix += ch;
                    cur = cur.Children[ch];
                }
                else
                {
                    fail = true;
                    break;
                }

            }

            if (cur.IsWord && !fail && result.Count < top)
                result.Add(prefix);

            top = (top == -1) ? _maxSearchCount : top;
            GetWords(cur, result, prefix, top);

            return result;
        }
        private void GetWords(Node cur, List<string> result, string prefix, int top)
        {
            if (cur.Children == null)
                return;

            foreach (Node node in cur.Children.Values)
            {
                string tmp = prefix + node.NodeKey;
                if (node.IsWord)
                {
                    if (result.Count >= top)
                        break;
                    else
                        result.Add(tmp);
                }
                GetWords(node, result, tmp, top);
            }
        }
    }
}