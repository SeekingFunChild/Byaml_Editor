using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ByamlEdit
{
    class EndianBytesOperator
    {
        public static uint readUInt(byte[] bytes,int start)
        {
            byte[] t_bytes = new byte[4];
            for(int i=0;i<4;i++)
            {
                t_bytes[i] = bytes[start + i];
            }
            Array.Reverse(t_bytes);
            return BitConverter.ToUInt32(t_bytes,0);
        }

        public static void writeUInt(byte[] bytes,int start,uint value)
        {
            byte[] t_bytes = BitConverter.GetBytes(value);
            Array.Reverse(t_bytes);
            for (int i = 0; i < 4; i++)
            {
                bytes[start+i] = t_bytes[i];
            }
        }
        
        public static string  readString(byte[] bytes,int start,int len)
        {
            string str="";
            for (int i = 0; i < len; i++)
            {
                str += (char)bytes[start + i];
            }
            return str;
        }

        public static string readString(byte[] bytes, int start)
        {
            string str = "";           
            for (int i = 0; ; i++)
            {
                var ch= (char)bytes[start + i];
                if(ch==0x0)
                {
                    break;
                }
                str += ch;
            }
            return str;
        }

        public static void writeString(byte[] bytes, int start, string str,int len)
        {
            char[] t_bytes = str.ToCharArray();
            for (int i = 0; i < len; i++)
            {
                bytes[start + i] = (byte)t_bytes[i];
            }
        }

        public static int writeString_PrOD(byte[] bytes,int start,string str)
        {
            char[] t_bytes = str.ToCharArray();
            int len = t_bytes.Length;
            for (int i=0;i<len;i++)
            {
                bytes[start + i] = (byte)t_bytes[i];
            }
            bytes[start+len] = 0x00;
            bytes[start+len + 1] = 0x00;
            len += 2;
            while(len%4!=0)
            {
                bytes[start+len] = 0x00;
                len++;
            }
            return len;
        }

        public static float readFloat(byte[] bytes,int start)
        {
            byte[] t_bytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                t_bytes[i] = bytes[start + i];
            }
            Array.Reverse(t_bytes);
            return BitConverter.ToSingle(t_bytes, 0);
        }

        public static void writeFloat(byte[] bytes,int start,float value)
        {
            byte[] t_bytes = BitConverter.GetBytes(value);
            Array.Reverse(t_bytes);
            for (int i = 0; i < 4; i++)
            {
                bytes[start + i] = t_bytes[i];
            }
        }
    }
}
