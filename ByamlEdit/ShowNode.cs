using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ByamlEdit
{
    class ShowNode:TreeNode
    {
        public ByamlNodeType Type;

        public ShowNode(string text):base(text)
        {
            
        }

        public ShowNode(JsonClip jsonClip) : base(jsonClip.Text)
        {
            Type = jsonClip.Type;
          
            foreach(var item in jsonClip.childs)
            {
                Nodes.Add(new ShowNode(item));
            }
        }
    }

    class JsonClip
    {
        public ByamlNodeType Type;
        public string Text;
        public List<JsonClip> childs;

        public JsonClip(ShowNode sNode)
        {
            childs = new List<JsonClip>();
            Type = sNode.Type;
            Text =sNode.Text;
            foreach (var node in sNode.Nodes)
            {
                childs.Add(new JsonClip((ShowNode)node));
            }
        }

        public JsonClip()
        {
            childs = new List<JsonClip>();
        }
    }
}
