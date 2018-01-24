using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ByamlEdit
{
    enum ByamlNodeType
    {
        String = 0xa0,
        UnamedNode = 0xc0,
        NamedNode = 0xc1,
        StringList = 0xc2,
        BinaryDataList = 0xc3,
        Boolean = 0xd0,
        UInt = 0xd1, //uint32
        Single = 0xd2,
        Hash =0xd3, //uint32
        Null = 0xff,
    }

    abstract class ByamlNode
    {
        public long Address { get; set; }
        public long Length { get; set; }

        public abstract ByamlNodeType Type { get; }
        public virtual bool CanBeAttribute { get { return false; } }

        public virtual void Show(ShowNode parent, List<string> nodes, List<string> values)
        {
            throw new NotImplementedException();
        }

        public class String : ByamlNode
        {
            public int Value { get; set; }

            public override ByamlNodeType Type
            {
                get { return ByamlNodeType.String; }
            }

            public String(EndianBinaryReader reader)
            {
                Address = reader.BaseStream.Position;

                Value = reader.ReadInt32();

                Length = reader.BaseStream.Position - Length;
            }

            public String(int value)
            {
                Value = value;
            }

            public override void Show(ShowNode parent, List<string> nodes, List<string> values)
            {
                ShowNode treeNode = new ShowNode(values[Value]);
                treeNode.Type= this.Type;
                parent.Nodes.Add(treeNode);
            }
        }

        public class Boolean : ByamlNode
        {
            public bool Value { get; set; }

            public override ByamlNodeType Type
            {
                get { return ByamlNodeType.Boolean; }
            }
            public override bool CanBeAttribute
            {
                get { return true; }
            }

            public Boolean(EndianBinaryReader reader)
            {
                Address = reader.BaseStream.Position;

                Value = reader.ReadInt32() != 0;

                Length = reader.BaseStream.Position - Length;
            }

            public Boolean(bool value)
            {
                Value = value;
            }

            public override void Show(ShowNode parent, List<string> nodes, List<string> values)
            {
                ShowNode treeNode = new ShowNode(Value.ToString());
                treeNode.Type = this.Type;
                parent.Nodes.Add(treeNode);
            }
        }

        public class Int : ByamlNode
        {
            public uint Value { get; set; }
            public override ByamlNodeType Type
            {
                get {
                    return ByamlNodeType.UInt;
                }
            }
            public override bool CanBeAttribute
            {
                get { return true; }
            }

            public Int(EndianBinaryReader reader)
            {
                Address = reader.BaseStream.Position;

                Value = reader.ReadUInt32();

                Length = reader.BaseStream.Position - Length;
            }

            public Int(uint value)
            {
                Value = value;
            }

            public override void Show(ShowNode parent, List<string> nodes, List<string> values)
            {
                ShowNode treeNode = new ShowNode(Value.ToString());
                treeNode.Type = this.Type;
                parent.Nodes.Add(treeNode);
            }
        }

        public class Hash : ByamlNode
        {
            public uint Value { get; set; }
            public override ByamlNodeType Type
            {
                get
                {
                    return ByamlNodeType.Hash;
                }
            }
            public override bool CanBeAttribute
            {
                get { return true; }
            }

            public Hash(EndianBinaryReader reader)
            {
                Address = reader.BaseStream.Position;

                Value = reader.ReadUInt32();

                Length = reader.BaseStream.Position - Length;
            }

            public Hash(uint value)
            {
                Value = value;
            }

            public override void Show(ShowNode parent, List<string> nodes, List<string> values)
            {
                ShowNode treeNode = new ShowNode(Value.ToString());
                treeNode.Type = this.Type;
                parent.Nodes.Add(treeNode);
            }
        }


        public class Single : ByamlNode
        {
            public float Value { get; set; }

            public override ByamlNodeType Type
            {
                get { return ByamlNodeType.Single; }
            }
            public override bool CanBeAttribute
            {
                get { return true; }
            }

            public Single(EndianBinaryReader reader)
            {
                Address = reader.BaseStream.Position;

                Value = reader.ReadSingle();

                Length = reader.BaseStream.Position - Length;
            }

            public Single(float value)
            {
                Value = value;
            }

            public override void Show(ShowNode parent, List<string> nodes, List<string> values)
            {
                ShowNode treeNode = new ShowNode(Value.ToString());
                treeNode.Type = this.Type;
                parent.Nodes.Add(treeNode);
            }
        }

        public class UnamedNode : ByamlNode
        {
            public override ByamlNodeType Type
            {
                get { return ByamlNodeType.UnamedNode; }
            }

            public Collection<ByamlNode> Nodes { get; private set; }

            public UnamedNode(EndianBinaryReader reader)
            {
                Address = reader.BaseStream.Position;

                Nodes = new Collection<ByamlNode>();

                //why
                if (Address == 0)
                {
                    return;
                }

                int count = reader.ReadInt32() & 0xffffff;
                byte[] types = reader.ReadBytes(count);

                while (reader.BaseStream.Position % 4 != 0)
                    reader.ReadByte();

                long start = reader.BaseStream.Position;

                for (int i = 0; i < count; i++)
                {
                    ByamlNodeType type = (ByamlNodeType)types[i];
                    switch (type)
                    {
                        case ByamlNodeType.String:
                            Nodes.Add(new String(reader));
                            break;
                        case ByamlNodeType.Boolean:
                            Nodes.Add(new Boolean(reader));
                            break;
                        case ByamlNodeType.UInt:
                            Nodes.Add(new Int(reader));
                            break;
                        case ByamlNodeType.Hash:
                            Nodes.Add(new Hash(reader));
                            break;
                        case ByamlNodeType.Single:
                            Nodes.Add(new Single(reader));
                            break;
                        case ByamlNodeType.UnamedNode:
                            reader.BaseStream.Position = reader.ReadInt32();
                            Nodes.Add(new UnamedNode(reader));
                            break;
                        case ByamlNodeType.NamedNode:
                            reader.BaseStream.Position = reader.ReadInt32();
                            Nodes.Add(new NamedNode(reader));
                            break;
                        default:
                            throw new InvalidDataException();
                    }

                    reader.BaseStream.Position = start + (i + 1) * 4;
                }

                Length = reader.BaseStream.Position - Length;
            }

            public UnamedNode()
            {
                Nodes = new Collection<ByamlNode>();
            }

            public override void Show(ShowNode parent, List<string> nodes, List<string> values)
            {
                foreach (var node in Nodes)
                {
                    ShowNode treeNode = new ShowNode("Value");
                    treeNode.Type = node.Type;
                    parent.Nodes.Add(treeNode);
                    node.Show(treeNode, nodes, values);
                 }
            }
        }

        public class NamedNode : ByamlNode
        {
            public override ByamlNodeType Type
            {
                get { return ByamlNodeType.NamedNode; }
            }

            public Collection<KeyValuePair<int, ByamlNode>> Nodes { get; private set; }

            public NamedNode(EndianBinaryReader reader)
            {
                Address = reader.BaseStream.Position;

                Nodes = new Collection<KeyValuePair<int, ByamlNode>>();

                int count = reader.ReadInt32() & 0xffffff;

                for (int i = 0; i < count; i++)
                {
                    uint temp = reader.ReadUInt32();
                    int name = (int)(temp >> 8);
                    ByamlNodeType type = (ByamlNodeType)(byte)temp;
                    switch (type)
                    {
                        case ByamlNodeType.String:
                            Nodes.Add(new KeyValuePair<int, ByamlNode>(name, new String(reader)));
                            break;
                        case ByamlNodeType.Boolean:
                            Nodes.Add(new KeyValuePair<int, ByamlNode>(name, new Boolean(reader)));
                            break;
                        case ByamlNodeType.UInt:
                            Nodes.Add(new KeyValuePair<int, ByamlNode>(name, new Int(reader)));
                            break;
                        case ByamlNodeType.Hash:
                            Nodes.Add(new KeyValuePair<int, ByamlNode>(name, new Hash(reader)));
                            break;
                        case ByamlNodeType.Single:
                            Nodes.Add(new KeyValuePair<int, ByamlNode>(name, new Single(reader)));
                            break;
                        case ByamlNodeType.UnamedNode:
                            reader.BaseStream.Position = reader.ReadInt32();
                            Nodes.Add(new KeyValuePair<int, ByamlNode>(name, new UnamedNode(reader)));
                            break;
                        case ByamlNodeType.NamedNode:
                            reader.BaseStream.Position = reader.ReadInt32();
                            Nodes.Add(new KeyValuePair<int, ByamlNode>(name, new NamedNode(reader)));
                            break;
                        default:
                            throw new InvalidDataException();
                    }

                    reader.BaseStream.Position = Address + (i + 1) * 8 + 4;
                }

                Length = reader.BaseStream.Position - Length;
            }

            public NamedNode()
            {
                Nodes = new Collection<KeyValuePair<int, ByamlNode>>();
            }


            public override void Show(ShowNode parent, List<string> nodes, List<string> values)
            {
                foreach(var node in Nodes)
                {
                    ShowNode treeNode = new ShowNode(nodes[node.Key]);
                    treeNode.Type = node.Value.Type;
                    parent.Nodes.Add(treeNode);
                    node.Value.Show(treeNode, nodes, values);
                }           
            }
        }

        public class StringList : ByamlNode
        {
            public Collection<string> Strings { get; private set; }

            public override ByamlNodeType Type
            {
                get { return ByamlNodeType.StringList; }
            }

            public StringList(EndianBinaryReader reader)
            {
                Address = reader.BaseStream.Position;

                Strings = new Collection<string>();

                int count = reader.ReadInt32() & 0xffffff;
                int[] offsets = reader.ReadInt32s(count);

                foreach (var item in offsets)
                {
                    reader.BaseStream.Seek(Address + item, SeekOrigin.Begin);
                    Strings.Add(reader.ReadStringNT(Encoding.ASCII));
                }

                Length = reader.BaseStream.Position - Length;
            }

            public StringList()
            {
                Strings = new Collection<string>();
            }
        }

      
    }


}
