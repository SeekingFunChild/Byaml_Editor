using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ByamlEdit
{
    class Yaz0
    {
        public static byte[] encode(byte[] src)
        {
            //find need compressed position
            uint curPos = 0;
            //List<KeyValuePair<uint, KeyValuePair<uint, uint>>> findCaches = new List<KeyValuePair<uint, KeyValuePair<uint, uint>>>();
            List<KeyValuePair<uint, KeyValuePair<uint, uint>>> finds = new List<KeyValuePair<uint, KeyValuePair<uint, uint>>>();
            while(curPos<src.Length)
            {
                var find = searchBefore(src, curPos);
                if(find.Value.Value!=0)
                {
                    finds.Add(find);      
                }
                //findCaches.Add(find);
                curPos = find.Key + find.Value.Value;
            }

            //compressed
            byte[] temp = new byte[src.Length+src.Length/8];
            uint writeLen = 0;
            uint srcPos = 0;
            uint dstPos = 0x01;
            uint opcodePos = 0;
            uint cycle = 0x00;
            foreach(var find in finds)
            {
                //write not need compressed data
                while(srcPos<find.Key)
                {
                    if (cycle == 8)
                    {
                        opcodePos = dstPos;
                        dstPos++;
                        cycle = 0x00;
                    }
                    temp[opcodePos] = (byte)(temp[opcodePos] | 0x80 >>(int)cycle);
                    cycle++;

                    temp[dstPos] = src[srcPos];
                    dstPos++;
                    srcPos++;
                }

                //write need compressed data
                if (cycle == 8)
                {
                    opcodePos = dstPos;
                    dstPos++;
                    cycle = 0x00;
                }
                cycle++;

                uint findLen = find.Value.Value;
                uint findPos = find.Value.Key;
                uint pos = find.Key;
                uint dist = pos - findPos-1;
               if(findLen<0x12)
                {
                    byte b1 = (byte)((findLen-2) << 4 | dist >> 8);
                    byte b2 = (byte)(dist & 0xFF);

                    temp[dstPos] = b1;
                    temp[dstPos + 1] = b2;
                    dstPos += 2;
                }
               else
                {
                    byte b1 = (byte)(dist >> 8);
                    byte b2 = (byte)(dist & 0xFF);
                    byte b3 = (byte)(findLen - 0x12);

                    temp[dstPos] = b1;
                    temp[dstPos + 1] = b2;
                    temp[dstPos + 2] = b3;
                    dstPos += 3;
                }

                srcPos += findLen;
            }

            //write not need compressed data
            while (srcPos < src.Length)
            {
                if (cycle == 8)
                {
                    opcodePos = dstPos;
                    dstPos++;
                    cycle = 0x00;
                }
                temp[opcodePos] = (byte)(temp[opcodePos] | 0x80 >> (int)cycle);
                cycle++;

                temp[dstPos] = src[srcPos];
                dstPos++;
                srcPos++;
            }

            writeLen = dstPos;

            byte[] ret = new byte[writeLen+0x10];
            EndianBytesOperator.writeString(ret, 0, "Yaz0", 4);
            EndianBytesOperator.writeUInt(ret, 0x04, (uint)src.Length);
            for(int i=0;i<writeLen;i++)
            {
                ret[i+0x10] = temp[i];
            }

            return ret;
        }

        private static KeyValuePair<uint, KeyValuePair<uint, uint>> searchBefore(byte[] bytes,uint curPos)
        {
            List<KeyValuePair<uint, KeyValuePair<uint, uint>>> finds = new List<KeyValuePair<uint, KeyValuePair<uint, uint>>>();

            int offset = 0;
            while(curPos+offset+3<bytes.Length)
            {
                byte[] sign = new byte[3];
                for (int i = 0; i < 3; i++)
                {
                    sign[i] = bytes[curPos + offset + i];
                }

                int findPos = -1;
                if(curPos+(uint)offset<0x0FFF)
                {
                    findPos = sundaySearch(bytes, 0, curPos + (uint)offset, sign);
                }
                else
                {
                    findPos = sundaySearch(bytes, curPos+(uint)offset-0x0FFF, curPos + (uint)offset, sign);
                }
                if(findPos!=-1)
                {
                    break;
                }

                offset++;
            }

            for(int i=0;i<0x03;i++)
            {
                for (int len = 0x03; len < 0xFF + 0x12; len++)
                {
                    if (curPos + len + offset+i > bytes.Length)
                    {
                        break;
                    }

                    byte[] sign = new byte[len];

                    for (int j = 0; j < len; j++)
                    {
                        sign[j] = bytes[curPos + offset +i+ j];
                    }

                    int findPos = -1;
                    if (curPos + (uint)offset < 0x0FFF)
                    {
                        findPos = sundaySearch(bytes, 0, curPos + (uint)offset+(uint)i, sign);
                    }
                    else
                    {
                        findPos = sundaySearch(bytes, curPos + (uint)offset - 0x0FFF, curPos + (uint)offset+(uint)i, sign);
                    }

                    if (findPos != -1)
                    {
                          finds.Add(new KeyValuePair<uint, KeyValuePair<uint, uint>>((uint)(curPos + offset + i), new KeyValuePair<uint, uint>((uint)findPos, (uint)len)));
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if(finds.Count>0)
            {
                finds.Sort(delegate(KeyValuePair<uint, KeyValuePair<uint, uint>> k1, KeyValuePair<uint, KeyValuePair<uint, uint>> k2) {
                    uint p1 = k1.Value.Value;
                    uint p2 = k2.Value.Value-(k2.Key-k1.Key);
                    if(p1==p2)
                    {
                        uint pos1 = k1.Key;
                        uint pos2 = k2.Key;
                        if (pos1 == pos2)
                        {
                            return 0;
                        }
                        else if (pos1 > pos2)
                        {
                            return -1;
                        }
                        else
                        {
                            return 1;
                        }
                    }

                    if (p1<p2)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                });
                return finds[finds.Count - 1];
            }
            else
            {
                return new KeyValuePair<uint, KeyValuePair<uint, uint>>((uint)(curPos+offset+3), new KeyValuePair<uint, uint>(0, 0));
            }
        }

        private static int sundaySearch(byte[] bytes, uint start, uint end, byte[] sign)
        {
            int[] charstep = new int[256];
            for(int i=0;i<256;i++)
            {
                charstep[i] = -1;
            }
            for(int i=0;i<sign.Length;i++)
            {
                charstep[(int)sign[i]] = i;
            }

            for(uint i=start;i<end;)
            {
                uint j = 0;
                while(j<sign.Length)
                {
                    if (bytes[i] == sign[j])
                    {
                        i++;
                        j++;
                    }
                    else
                    {
                        uint pos = i + (uint)sign.Length-j;
                        if (charstep[bytes[pos]] == -1)
                        {
                            i = pos + 1;
                        }
                        else
                        {
                            i = pos - (uint)charstep[bytes[pos]];
                        }
                        break;
                    }
                }

                if(j==sign.Length)
                {
                    return (int)(i - sign.Length);
                }
            }

            return -1;     
        }
        
        public static byte[] decode(byte[] src)
        {
            if(!IsYaz0(src))
            {
                return src;
            }

            uint decompressedSize = EndianBytesOperator.readUInt(src, 0x04);
            byte[] dst = new byte[decompressedSize];
            uint srcPos = 0x10;
            uint dstPos = 0;

            byte Opcode = 0x00;
            //List<KeyValuePair<int, KeyValuePair<int, int>>> finds = new List<KeyValuePair<int, KeyValuePair<int, int>>>();
            while(true)
            {
                Opcode = src[srcPos];
                srcPos++;
                for(int i=0;i<8;i++,Opcode<<=1)
                {
                    if((Opcode&0x80)!=0)
                    {
                        dst[dstPos] = src[srcPos];
                        dstPos++;
                        srcPos++;
                    }
                    else
                    {
                        byte b1 = src[srcPos];
                        byte b2 = src[srcPos + 1];
                        srcPos += 2;

                        uint copyLen = 0;
                        if((b1>>4)==0)
                        {
                            copyLen = (uint)src[srcPos] + 0x12;
                            srcPos++;
                        }
                        else
                        {
                            copyLen = (uint)(b1 >> 4) + 0x02;
                        }
                        uint dist = (uint)((b1 & 0x0F) << 8 | b2)+1;
                        uint copyPos = dstPos - dist;
                       // finds.Add(new KeyValuePair<int, KeyValuePair<int, int>>((int)dstPos, new KeyValuePair<int, int>((int)copyPos, (int)copyLen)));
                        for(int j=0;j<copyLen;j++)
                        {
                            dst[dstPos] = dst[copyPos];
                            dstPos++;
                            copyPos++;
                        }
                    }

                    if (dstPos >= decompressedSize)
                    {
                        return dst;
                    }
                }
            }
        }

        public static bool IsYaz0(byte[] bytes)
        {
           return EndianBytesOperator.readString(bytes,0,4)== "Yaz0";
        }
    }
}
