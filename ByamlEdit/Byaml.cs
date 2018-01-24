using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace ByamlEdit
{
    class Byaml
    {
        string path;

        bool isYaz0 = false;
        List<string> nodes;
        List<string> values;
        ByamlNode tree;


        #region controls
        TreeView treeView;
        ShowNode root;
        TreeNode nodeEdited;
        Button Save;
        TextBox SearchText;
        Button Search;
        ListView SearchResults;

        Button Copy;
        Button Paste;

        ComboBox NewNodeType;
        Button NewNode;
        Button DeleteNode;
        Button ChangeType;
        #endregion

        List<TreeNode> searched_nodes;
        string ClipText;

        //node chache
        List<KeyValuePair<int, ShowNode>> nodeChaches;

        public Byaml(string byamlFile)
        {
            nodes = new List<string>();
            values = new List<string>();

            nodeChaches = new List<KeyValuePair<int, ShowNode>>();

            searched_nodes = new List<TreeNode>();

            byte[] bytes = File.ReadAllBytes(byamlFile);
            isYaz0 = Yaz0.IsYaz0(bytes);
            if(isYaz0)
            {
                bytes = Yaz0.decode(bytes);
            }

            var stream = new MemoryStream(bytes);
            var reader = new EndianBinaryReader(stream);
            reader.Endianness = Endianness.BigEndian;

            if (reader.ReadUInt16() != 0x4259)
                throw new InvalidDataException();
            if (reader.ReadUInt16() != 0x0002)
                throw new InvalidDataException();

            path = byamlFile;

            uint nodeOffset = reader.ReadUInt32();
            if (nodeOffset > reader.BaseStream.Length)
                throw new InvalidDataException();

            uint valuesOffset = reader.ReadUInt32();
            if (valuesOffset > reader.BaseStream.Length)
                throw new InvalidDataException();

            uint treeOffset = reader.ReadUInt32();
            if (treeOffset > reader.BaseStream.Length)
                throw new InvalidDataException();

            if (nodeOffset != 0)
            {
                reader.BaseStream.Seek(nodeOffset, SeekOrigin.Begin);
                nodes.AddRange(new ByamlNode.StringList(reader).Strings);
            }
            if (valuesOffset != 0)
            {
                reader.BaseStream.Seek(valuesOffset, SeekOrigin.Begin);
                values.AddRange(new ByamlNode.StringList(reader).Strings);
            }

            ByamlNodeType rootType;
            reader.BaseStream.Seek(treeOffset, SeekOrigin.Begin);
            rootType = (ByamlNodeType)reader.ReadByte();
            reader.BaseStream.Seek(-1, SeekOrigin.Current);
            if (rootType == ByamlNodeType.UnamedNode)
                tree = new ByamlNode.UnamedNode(reader);
            else
                tree = new ByamlNode.NamedNode(reader);

            root = new ShowNode("root");
            root.Type = tree.Type;

            reader.Close();
        }

        public void Show(Form control)
        {
            treeView = new TreeView();
            treeView.Width = control.Width - 200;
            treeView.Height = control.Height - 100;

            treeView.LabelEdit = true;
            treeView.NodeMouseClick += treeView_NodeMouseClick;
            treeView.AfterLabelEdit += treeView_AfterLabelEdit;


            SearchText = new TextBox();
            SearchText.Top = treeView.Bottom + 10;
            SearchText.Left = SearchText.Left;
            control.Controls.Add(SearchText);

            Search = new Button();
            Search.Text = "Search";
            Search.Top = treeView.Bottom + 10;
            Search.Left = SearchText.Left + SearchText.Width + 10;
            Search.Click += SearchClick;
            control.Controls.Add(Search);

            SearchResults = new ListView();
            SearchResults.Top = treeView.Top;
            SearchResults.Left = treeView.Right + 10;
            SearchResults.Height = treeView.Height;
            SearchResults.Width = 160;
            SearchResults.View = View.Details;
            SearchResults.Columns.Add("index", 60, HorizontalAlignment.Left);
            SearchResults.Columns.Add("Value", 200, HorizontalAlignment.Left);
            SearchResults.FullRowSelect = true;
            SearchResults.MouseClick += ListView_Click;
            control.Controls.Add(SearchResults);

            //node type combox
            NewNodeType = new ComboBox();
            NewNodeType.Top = treeView.Bottom + 10;
            NewNodeType.Left = Search.Right + 10;
            var item = new CustomItem();
            item.Text = "UInt";
            item.Value = ByamlNodeType.UInt;
            NewNodeType.Items.Add(item);

            item = new CustomItem();
            item.Text = "Hash";
            item.Value = ByamlNodeType.Hash;
            NewNodeType.Items.Add(item);

            item = new CustomItem();
            item.Text = "Single";
            item.Value = ByamlNodeType.Single;
            NewNodeType.Items.Add(item);

            item = new CustomItem();
            item.Text = "Bool";
            item.Value = ByamlNodeType.Boolean;
            NewNodeType.Items.Add(item);

            item = new CustomItem();
            item.Text = "String";
            item.Value = ByamlNodeType.String;
            NewNodeType.Items.Add(item);

            item = new CustomItem();
            item.Text = "NamedNode";
            item.Value = ByamlNodeType.NamedNode;
            NewNodeType.Items.Add(item);

            item = new CustomItem();
            item.Text = "UnamedNode";
            item.Value = ByamlNodeType.UnamedNode;
            NewNodeType.Items.Add(item);

            NewNodeType.SelectedItem = item;
            control.Controls.Add(NewNodeType);

            //new
            NewNode = new Button();
            NewNode.Text = "NewNode";
            NewNode.Left = NewNodeType.Right + 10;
            NewNode.Top = treeView.Bottom + 10;
            NewNode.Click += NewClick;
            control.Controls.Add(NewNode);

            //delete
            DeleteNode = new Button();
            DeleteNode.Text = "DeleteNode";
            DeleteNode.Left = NewNode.Right + 10;
            DeleteNode.Top = treeView.Bottom + 10;
            DeleteNode.Click += DeleteClick;
            control.Controls.Add(DeleteNode);

            //change type
            ChangeType = new Button();
            ChangeType.Text = "ChangType";
            ChangeType.Left = DeleteNode.Right + 10;
            ChangeType.Top = treeView.Bottom + 10;
            ChangeType.Click += ChangeTypeClick;
            control.Controls.Add(ChangeType);

            //copy
            Copy = new Button();
            Copy.Text = "Copy";
            Copy.Top += treeView.Bottom + 10;
            Copy.Left = ChangeType.Right + 10;
            Copy.Click += CopyClick;
            control.Controls.Add(Copy);

            //paste
            Paste = new Button();
            Paste.Text = "Paste";
            Paste.Top += treeView.Bottom + 10;
            Paste.Left = Copy.Right + 10;
            Paste.Click += PasteClick;
            control.Controls.Add(Paste);

            //save
            Save = new Button();
            Save.Text = "Save";
            Save.Top += treeView.Bottom + 10;
            Save.Left = Paste.Right + 10;
            Save.Click += SaveClick;
            control.Controls.Add(Save);

            control.Controls.Add(treeView);

            treeView.Nodes.Add(root);
            tree.Show(root, nodes, values);
        }

        #region Form Events Handler
        private void treeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                    e.Node.BeginEdit();
                foreach(var item in NewNodeType.Items)
                {
                    if (((CustomItem)item).Value == ((ShowNode)e.Node).Type)
                    {
                        NewNodeType.SelectedItem = item;
                    }
                }  
            }
        }

        private void treeView_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            nodeEdited = e.Node;
            e.Node.EndEdit(true);
        }

        private void NewClick(object sender, EventArgs e)
        {
            TreeNode nodeSelected = treeView.SelectedNode;
            ShowNode newNode = new ShowNode("new"+ ((CustomItem)NewNodeType.SelectedItem).Text);
            newNode.Type = ((CustomItem)NewNodeType.SelectedItem).Value;
            nodeSelected.Nodes.Add(newNode);
        }

        private void ChangeTypeClick(object sender, EventArgs e)
        {
            ShowNode nodeSelected = (ShowNode)treeView.SelectedNode;
            nodeSelected.Type= ((CustomItem)NewNodeType.SelectedItem).Value;
        }

        private void DeleteClick(object sender, EventArgs e)
        {
            TreeNode nodeSelected = treeView.SelectedNode;
            TreeNode parent = nodeSelected.Parent;
            parent.Nodes.Remove(nodeSelected);
        }

        private void ListView_Click(object sender, MouseEventArgs e)
        {
            int index = int.Parse(SearchResults.FocusedItem.Text);
            var node = searched_nodes[index];
            treeView.SelectedNode = node;
            treeView.Focus();
        }

        private void SearchClick(object sender,EventArgs e)
        {
            searched_nodes.Clear();
            SearchTreeView(SearchText.Text);
            SearchResults.Items.Clear();

            if(searched_nodes.Count==0)
            {
                MessageBox.Show("not found");
                return;
            }

            int index = 0;
            SearchResults.BeginUpdate();
            foreach(TreeNode treeNode in searched_nodes)
            {
                treeView.SelectedNode = treeNode;
                treeView.Focus();

                ListViewItem lvi = new ListViewItem();
                lvi.Text = index.ToString();
                lvi.SubItems.Add(treeNode.Text);
                SearchResults.Items.Add(lvi);

                index++;
            }
            SearchResults.EndUpdate();
        }

        private void SearchTreeView(string text)
        {
            if(text!=null&&text!="")
            {
                foreach(TreeNode node in treeView.Nodes)
                {
                    if(node.Text.ToLower().IndexOf(text.ToLower()) !=-1)
                    {
                        searched_nodes.Add(node);
                    }
                }
                foreach (TreeNode node in treeView.Nodes)
                {
                    SearchTreeNode(node, text);
                }
            }
        }

        private void SearchTreeNode(TreeNode root,string text)
        {
            if (text != null && text != "")
            {
                foreach (TreeNode node in root.Nodes)
                {
                    if (node.Text.ToLower().IndexOf(text.ToLower()) != -1)
                    {
                        searched_nodes.Add(node);
                    }
                }
                foreach (TreeNode node in root.Nodes)
                {
                    SearchTreeNode(node, text);
                }
            }
        }

        private void CopyClick(object sender, EventArgs e)
        {
            var jsonClip=new JsonClip((ShowNode)treeView.SelectedNode);
            ClipText =JsonConvert.SerializeObject(jsonClip);
            Clipboard.SetDataObject(ClipText);
            MessageBox.Show("copy done");
        }

        private void PasteClick(object sender, EventArgs e)
        {
            IDataObject iData = Clipboard.GetDataObject();
            if (iData.GetDataPresent(DataFormats.Text))
            {
                ClipText=(string)iData.GetData(DataFormats.Text);
                var jsonClip = JsonConvert.DeserializeObject<JsonClip>(ClipText);
                var showNode = new ShowNode(jsonClip);
                treeView.SelectedNode.Nodes.Add(showNode);
                treeView.SelectedNode = showNode;
                treeView.Focus();
            }
        }
        #endregion

        #region byamlWriter
        private int CustomStringSort(string s1, string s2)
        {
            bool longer = s1.Length > s2.Length;
            bool lenEqual = s1.Length == s2.Length;
            int len = longer ? s2.Length : s1.Length;
            for (int i = 0; i < len; ++i)
            {
                if (s1[i] >= "a".ToArray()[0] && s2[i] >= "a".ToArray()[0])
                {
                    if (s1[i] >= "A".ToArray()[0] && s2[i] < "A".ToArray()[0])
                    {
                        return -1;
                    }
                    else if (s1[i] < "A".ToArray()[0] && s2[i] >= "A".ToArray()[0])
                    {
                        return 1;
                    }
                }


                if (s1[i] > s2[i])
                {
                    return 1;
                }
                else if (s1[i] < s2[i])
                {
                    return -1;
                }
            }

            if (lenEqual)
            {
                return 0;
            }
            else if (longer)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

        private void SaveClick(object sender, EventArgs e)
        {
            byte[] temp = new byte[20 * 1024 * 1024];
            using (EndianBinaryWriter writer = new EndianBinaryWriter(new MemoryStream(temp)))
            {
                nodes.Clear();
                values.Clear();
                UpdateStrings(root);
                nodes.Sort(CustomStringSort);
                values.Sort(CustomStringSort);

                nodeChaches.Clear();
                int len=SaveTreeToFile(root, writer);
                byte[] bytes = new byte[len];
                for(int i=0;i<len;i++)
                {
                    bytes[i] = temp[i];
                }

                if(isYaz0)
                {
                    bytes = Yaz0.encode(bytes);
                }

                File.WriteAllBytes(path, bytes);

                writer.Close();
            }
            MessageBox.Show("save successful");
        }

        //更新nodes，values中的字符串
        private void UpdateStrings(ShowNode rootNode)
        {
            switch (rootNode.Type)
            {
                case ByamlNodeType.String:
                    if (!values.Contains(rootNode.Nodes[0].Text))
                    {
                        values.Add(rootNode.Nodes[0].Text);
                    }
                    break;
                case ByamlNodeType.Boolean:
                case ByamlNodeType.UInt:
                case ByamlNodeType.Hash:
                case ByamlNodeType.Single:
                    break;
                case ByamlNodeType.UnamedNode:
                    foreach (ShowNode node in rootNode.Nodes)
                    {
                        UpdateStrings(node);
                    }
                    break;
                case ByamlNodeType.NamedNode:
                    foreach (ShowNode node in rootNode.Nodes)
                    {
                        if (!nodes.Contains(node.Text))
                        {
                            nodes.Add(node.Text);
                        }
                        UpdateStrings(node);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void WriteString(EndianBinaryWriter writer, string str)
        {
            byte[] bytes = System.Text.UTF8Encoding.Default.GetBytes(str);
            writer.Write(bytes, 0, bytes.Length);
        }

        private int CalCount(List<string> list)
        {
            int count = 0;
            int length = 0;
            foreach (var str in list)
            {
                length += str.Length + 1;
                count++;
            }
            if (length % 4 != 0)
            {
                count++;
            }

            return count;
        }

        private bool TreeNodeCmp(TreeNode n1, TreeNode n2)
        {
            if (n1.Text == n2.Text && n1.Nodes.Count == n2.Nodes.Count)
            {
                if (n1.Nodes.Count == 0)
                {
                    return true;
                }
                else
                {
                    for (int i = 0; i < n1.Nodes.Count; ++i)
                    {
                        if (!TreeNodeCmp(n1.Nodes[i], n2.Nodes[i]))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        private void WriteByamlNodes(ShowNode root, int offset, EndianBinaryWriter writer)
        {
            writer.BaseStream.Seek(offset, SeekOrigin.Begin);

            bool Existed = false;
            int existedOffset = 0;


            int value = 0;
            int p0 = 0;
            int p1 = 0;
            int p2 = 0;
            ByamlNodeType type = root.Type;
            switch (type)
            {
                case ByamlNodeType.UnamedNode:
                    //写入头部
                    value = 0xc0 << 24 | root.Nodes.Count;
                    writer.Write(value);

                    //p0:type区域的写入偏移 p1:节点地址区域的写入偏移 p2:节点区域的写入偏移
                    p0 = (int)writer.BaseStream.Position;

                    int lenght = root.Nodes.Count;
                    while (lenght % 4 != 0)
                    {
                        lenght++;
                    }
                    p1 = (int)writer.BaseStream.Position + lenght;
                    p2 = p1 + 4 * root.Nodes.Count;

                    foreach (ShowNode node in root.Nodes)
                    {
                        //写入类型
                        writer.BaseStream.Seek(p0, SeekOrigin.Begin);
                        writer.Write((byte)node.Type);
                        p0 = (int)writer.BaseStream.Position;

                        //写入节点
                        switch (node.Type)
                        {
                            case ByamlNodeType.String:
                                writer.BaseStream.Seek(p1, SeekOrigin.Begin);
                                writer.Write(values.IndexOf(node.Nodes[0].Text));
                                p1 = (int)writer.BaseStream.Position;
                                break;
                            case ByamlNodeType.Boolean:
                                writer.BaseStream.Seek(p1, SeekOrigin.Begin);
                                value = bool.Parse(node.Nodes[0].Text) ? 1 : 0;
                                writer.Write(value);
                                p1 = (int)writer.BaseStream.Position;
                                break;
                            case ByamlNodeType.UInt:
                                writer.BaseStream.Seek(p1, SeekOrigin.Begin);
                                writer.Write(uint.Parse(node.Nodes[0].Text));
                                p1 = (int)writer.BaseStream.Position;
                                break;
                            case ByamlNodeType.Hash:
                                writer.BaseStream.Seek(p1, SeekOrigin.Begin);
                                writer.Write(uint.Parse(node.Nodes[0].Text));
                                p1 = (int)writer.BaseStream.Position;
                                break;
                            case ByamlNodeType.Single:
                                writer.BaseStream.Seek(p1, SeekOrigin.Begin);
                                writer.Write(float.Parse(node.Nodes[0].Text));
                                p1 = (int)writer.BaseStream.Position;
                                break;
                            case ByamlNodeType.UnamedNode:
                            case ByamlNodeType.NamedNode:
                                Existed = false;
                                foreach (var item in nodeChaches)
                                {
                                    if (TreeNodeCmp(item.Value, node))
                                    {
                                        Existed = true;
                                        existedOffset = item.Key;
                                    }
                                }
                                if (Existed)
                                {
                                    //写入节点地址
                                    writer.BaseStream.Seek(p1, SeekOrigin.Begin);
                                    writer.Write(existedOffset);
                                    p1 = (int)writer.BaseStream.Position;
                                }
                                else
                                {
                                    nodeChaches.Add(new KeyValuePair<int, ShowNode>(p2, node));

                                    //写入节点地址
                                    writer.BaseStream.Seek(p1, SeekOrigin.Begin);
                                    writer.Write(p2);
                                    p1 = (int)writer.BaseStream.Position;

                                    //写入节点数据
                                    writer.BaseStream.Seek(p2, SeekOrigin.Begin);
                                    WriteByamlNodes(node, p2, writer);
                                    p2 = (int)writer.BaseStream.Position;
                                }
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }

                    writer.BaseStream.Seek(p2, SeekOrigin.Begin);
                    break;
                case ByamlNodeType.NamedNode:
                    //写入头部
                    value = 0xc1 << 24 | root.Nodes.Count;
                    writer.Write(value);

                    //p0:节点基本信息区域写入偏移 p1:节点实例写入区域区域偏移
                    p0 = (int)writer.BaseStream.Position;
                    p1 = (int)writer.BaseStream.Position + 8 * root.Nodes.Count;


                    foreach (ShowNode node in root.Nodes)
                    {
                        //写入节点信息头部
                        writer.BaseStream.Seek(p0, SeekOrigin.Begin);
                        writer.Write(nodes.IndexOf(node.Text) << 8 | (int)node.Type);
                        //写入节点的值或地址
                        switch (node.Type)
                        {
                            case ByamlNodeType.String:
                                writer.Write(values.IndexOf(node.Nodes[0].Text));
                                p0 = (int)writer.BaseStream.Position;
                                break;
                            case ByamlNodeType.Boolean:
                                value = bool.Parse(node.Nodes[0].Text) ? 1 : 0;
                                writer.Write(value);
                                p0 = (int)writer.BaseStream.Position;
                                break;
                            case ByamlNodeType.UInt:
                                writer.Write(uint.Parse(node.Nodes[0].Text));
                                p0 = (int)writer.BaseStream.Position;
                                break;
                            case ByamlNodeType.Hash:
                                writer.Write(uint.Parse(node.Nodes[0].Text));
                                p0 = (int)writer.BaseStream.Position;
                                break;
                            case ByamlNodeType.Single:
                                writer.Write(float.Parse(node.Nodes[0].Text));
                                p0 = (int)writer.BaseStream.Position;
                                break;
                            case ByamlNodeType.UnamedNode:
                            case ByamlNodeType.NamedNode:
                                Existed = false;
                                foreach (var item in nodeChaches)
                                {
                                    if (TreeNodeCmp(item.Value, node))
                                    {
                                        Existed = true;
                                        existedOffset = item.Key;
                                    }
                                }
                                if (Existed)
                                {
                                    //写入节点的实例地址
                                    writer.Write(existedOffset);
                                    p0 = (int)writer.BaseStream.Position;
                                }
                                else
                                {
                                    nodeChaches.Add(new KeyValuePair<int, ShowNode>(p1, node));

                                    //写入节点的实例地址
                                    writer.Write(p1);
                                    p0 = (int)writer.BaseStream.Position;
                                    //写入节点实例
                                    WriteByamlNodes(node, p1, writer);
                                    p1 = (int)writer.BaseStream.Position;
                                }
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }

                    writer.BaseStream.Seek(p1, SeekOrigin.Begin);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void WriteStringList(long offset, List<string> strList, EndianBinaryWriter writer)
        {
            int strsAddressPtr = 0;
            int strsStrPtr = 0;

            writer.BaseStream.Seek(offset, SeekOrigin.Begin);
            int value = 0xc2 << 24 | strList.Count;
            writer.Write(value);
            strsAddressPtr = (int)writer.BaseStream.Position;
            strsStrPtr = (int)writer.BaseStream.Position + 4 * (strList.Count + 1);


            for (int i = 0; i < strList.Count; ++i)
            {
                //写入字符串地址
                writer.BaseStream.Seek(strsAddressPtr, SeekOrigin.Begin);
                int relative = strsStrPtr - (int)offset;
                writer.Write(relative);
                strsAddressPtr = (int)writer.BaseStream.Position;

                //写入字符串
                writer.BaseStream.Seek(strsStrPtr, SeekOrigin.Begin);
                WriteString(writer, strList[i]);
                writer.Write((byte)0);
                strsStrPtr = (int)writer.BaseStream.Position;
            }
            //写入字符串的结尾地址
            writer.BaseStream.Seek(strsAddressPtr, SeekOrigin.Begin);
            writer.Write(strsStrPtr - (int)offset);

            //填充字符串长度到4的倍数
            writer.BaseStream.Seek(strsStrPtr, SeekOrigin.Begin);
            while (writer.BaseStream.Position % 4 != 0)
            {
                writer.Write((byte)0);
            }
        }

        private int SaveTreeToFile(ShowNode rootNode, EndianBinaryWriter writer)
        {
            long headerPtr = 0;
            //writer magic
            WriteString(writer, "BY");
            //version
            writer.Write((UInt16)0x0002);
            headerPtr = writer.BaseStream.Position;
            //nodes
            int nodesOffset = 0x10;
            writer.Write(nodesOffset);
            headerPtr = writer.BaseStream.Position;
            WriteStringList(nodesOffset, nodes, writer);
            //values
            int valuesOffset = (int)writer.BaseStream.Position;
            writer.BaseStream.Seek(headerPtr, SeekOrigin.Begin);
            writer.Write(valuesOffset);
            headerPtr = writer.BaseStream.Position;
            WriteStringList(valuesOffset, values, writer);
            //tree
            int treeOffset = (int)writer.BaseStream.Position;
            writer.BaseStream.Seek(headerPtr, SeekOrigin.Begin);
            writer.Write(treeOffset);
            WriteByamlNodes(rootNode, treeOffset, writer);

            return (int)writer.BaseStream.Position;
        }
        #endregion
    }
}
