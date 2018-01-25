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
            uint endPos = (uint)src.Length;
            List<KeyValuePair<uint, KeyValuePair<uint, uint>>> finds = new List<KeyValuePair<uint, KeyValuePair<uint, uint>>>();
            while(curPos+0x03<=endPos)
            {
                var find = searchNextNCPoses(src,curPos,endPos);
                if(find[0].Value.Value!=0)
                {
                    finds.AddRange(find);      
                }
                curPos = find[find.Count-1].Key + find[find.Count-1].Value.Value;
            }

            //compress
            byte[] temp = new byte[src.Length+src.Length/8];
            uint writeLen = 0;
            uint srcPos = 0;
            uint dstPos = 0x01;
            uint opcodePos = 0;
            uint cycle = 0x00;
            foreach(var find in finds)
            {
                //write data that does not need to be compressed 
                while (srcPos<find.Key)
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

                //write data that need to compressed
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

            //write data that does not need to be compressed 
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

        private static List<KeyValuePair<uint, KeyValuePair<uint, uint>>> searchNextNCPoses(byte[] bytes,uint curPos,uint endPos)
        {

            uint offset = 0;

            //find first position that has match  start from curPos
            byte[] sign = new byte[3];
            List<uint> matchedPoses = new List<uint>();
            while (curPos+offset+3<=endPos)
            {
                for (int i = 0; i < 3; i++)
                {
                    sign[i] = bytes[curPos + offset + i];
                }

                if(curPos+offset<0x0FFF)
                {
                    matchedPoses= sundaySearch(bytes, 0, curPos + offset, sign);
                }
                else
                {
                    matchedPoses = sundaySearch(bytes, curPos + offset - 0x0FFF, curPos + offset, sign);
                }
                if(matchedPoses.Count!=0)
                {
                    break;
                }

                offset++;
            }

            List<KeyValuePair<uint, KeyValuePair<uint, uint>>> finds = new List<KeyValuePair<uint, KeyValuePair<uint, uint>>>();
            uint t_pos = curPos + offset;
            finds.Add(new KeyValuePair<uint, KeyValuePair<uint, uint>>(t_pos,getLongestMatch(bytes,t_pos,0x03)));
            t_pos = curPos + offset+1;
            if(t_pos+0x3<=endPos)
            {
                var retMatch = getLongestMatch(bytes, t_pos, finds.Last().Value.Value);
                if(retMatch.Value!=0)
                {
                    finds.Add(new KeyValuePair<uint, KeyValuePair<uint, uint>>(t_pos, retMatch));
                }
            }

            List<KeyValuePair<uint, KeyValuePair<uint, uint>>> ret = new List<KeyValuePair<uint, KeyValuePair<uint, uint>>>();
            finds.Sort(delegate (KeyValuePair<uint, KeyValuePair<uint, uint>> k1, KeyValuePair<uint, KeyValuePair<uint, uint>> k2) {
                uint p1 = 0;
                uint p2 = 0;

                p1 = k1.Value.Value;
                p2 = k2.Value.Value-(k2.Key-k1.Key);

                if (p1 == p2)
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
                if (p1 < p2)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            });

            ret.Add(finds.Last());
              
            return ret;
        }

        private static KeyValuePair<uint, uint>  getLongestMatch(byte[] bytes,uint curPos,uint length)
        {
            byte[] sign = new byte[length];
            List<uint> matchedPoses = new List<uint>();
            List<KeyValuePair<uint, uint>> matches = new List<KeyValuePair<uint, uint>>();

            for (int i = 0; i < length; i++)
            {
                sign[i] = bytes[curPos + i];
            }
            if (curPos < 0x0FFF)
            {
                matchedPoses = sundaySearch(bytes, 0, curPos, sign);
            }
            else
            {
                matchedPoses = sundaySearch(bytes, curPos - 0x0FFF, curPos, sign);
            }
            //calculate each matched length in matched pos
            foreach (var pos in matchedPoses)
            {
                matches.Add(new KeyValuePair<uint, uint>(pos, calLength(bytes, pos, curPos)));
            }
            //sort by length
            matches.Sort(delegate (KeyValuePair<uint, uint> p1, KeyValuePair<uint, uint> p2)
            {
                if (p1.Value == p2.Value)
                {
                    return 0;
                }
                else if (p1.Value > p2.Value)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            });

            if(matches.Count==0)
            {
                return new KeyValuePair<uint, uint>(0, 0);
            }
            return matches.Last();
        }

        private static List<uint> sundaySearch(byte[] bytes, uint start, uint end, byte[] sign)
        {
            int[] charstep = new int[256];
            for(int i=0;i<256;i++)
            {
                charstep[i] = -1;
            }
            for(int i=0;i<sign.Length;i++)
            {
                charstep[sign[i]] = i;
            }

            List<uint> ret = new List<uint>();
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
                    ret.Add(i-j);
                    i = i - j + 1;
                }
            }

            return ret;     
        }
        
        private static uint calLength(byte[] bytes,uint findPos,uint curPos)
        {
            uint len = 3;
            for(int index=3;index<0xFF+0x12&&curPos+index<bytes.Length;index++)
            {
                    if(bytes[findPos+index]==bytes[curPos+index])
                    {
                        len++;
                    }
                    else
                    {
                        break;
                    }
            }
            return len;
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
            List<KeyValuePair<int, KeyValuePair<int, int>>> finds = new List<KeyValuePair<int, KeyValuePair<int, int>>>();
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
                        finds.Add(new KeyValuePair<int, KeyValuePair<int, int>>((int)dstPos, new KeyValuePair<int, int>((int)copyPos, (int)copyLen)));
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
