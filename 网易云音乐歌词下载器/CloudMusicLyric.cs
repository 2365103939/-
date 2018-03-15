using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace 网易云音乐歌词下载器
{
    internal static class CloudMusicLyric
    {
        private static readonly byte[] _163Start = Encoding.UTF8.GetBytes("163 key(Don't modify):");
        private static readonly byte[] _163EndMp3 = { 0x54, 0x41, 0x4C, 0x42 };
        private static readonly byte[] _163EndFlac = { 0x0, 0x0, 0x0, 0x45 };
        private static readonly byte[] Key = Encoding.UTF8.GetBytes(@"#14ljk_!\]&0U<'(");
        private static readonly AesCryptoServiceProvider AesDecryptor = new AesCryptoServiceProvider
        {
            BlockSize = 128,
            Key = Key,
            Mode = CipherMode.ECB,
            Padding = PaddingMode.PKCS7
        };
        private static readonly byte[] UnCollectedFlag = Encoding.UTF8.GetBytes("\"uncollected\":true");
        private static readonly byte[] NoLyricFlag = Encoding.UTF8.GetBytes("\"nolyric\":true");
        private static readonly byte[] Lrc = Encoding.UTF8.GetBytes("\"lrc\":{\"");
        private static readonly byte[] TLyric = Encoding.UTF8.GetBytes("\"tlyric\":{\"");
        private static readonly byte[] Lyric = Encoding.UTF8.GetBytes("\"lyric\":");
        private static readonly byte[] LyricEnd = Encoding.UTF8.GetBytes("},");

        public static byte[] Get163Key(string file, bool isMp3)
        {
            byte[] bytFile;
            int startIndex;
            int endIndex;
            byte[] byt163Key;

            bytFile = new byte[0x4000];
            using (FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                fileStream.Read(bytFile, 0, bytFile.Length);
            startIndex = GetIndex(bytFile, _163Start, 0);
            if (startIndex == -1)
                return null;
            if (isMp3)
                endIndex = GetIndex(bytFile, _163EndMp3, startIndex);
            else
                endIndex = GetIndex(bytFile, _163EndFlac, startIndex) - 1;
            if (endIndex == -1)
                return null;
            byt163Key = new byte[endIndex - startIndex - _163Start.Length];
            Buffer.BlockCopy(bytFile, startIndex + _163Start.Length, byt163Key, 0, byt163Key.Length);
            return byt163Key;
        }

        public static string GetMusicId(byte[] byt163Key)
        {
            byt163Key = Convert.FromBase64String(Encoding.UTF8.GetString(byt163Key));
            byt163Key = AesDecryptor.CreateDecryptor().TransformFinalBlock(byt163Key, 0, byt163Key.Length);
            return Encoding.UTF8.GetString(byt163Key, 17, GetIndex(byt163Key, 0x2C, 17) - 17);
        }

        public static byte[] GetLyric(string musicId)
        {
            return GetWebSrc("http://music.163.com/api/song/lyric?os=pc&id=" + musicId + "&lv=-1&tv=-1");
        }

        public static bool IsCollected(byte[] bytLyric)
        {
            return GetIndex(bytLyric, UnCollectedFlag, 1, 1) != 1;
        }

        public static bool HasLyric(byte[] bytLyric)
        {
            return GetIndex(bytLyric, NoLyricFlag, 1, 1) != 1;
        }

        public static string GetOriginalLyric(byte[] bytLyric)
        {
            int startIndex;
            int endIndex;
            int subStartIndex;
            string strLrc;

            startIndex = GetIndex(bytLyric, Lrc, 0);
            endIndex = GetIndex(bytLyric, LyricEnd, startIndex);
            subStartIndex = GetIndex(bytLyric, Lyric, startIndex, endIndex) + Lyric.Length;
            strLrc = Encoding.UTF8.GetString(bytLyric, subStartIndex, endIndex - subStartIndex);
            return strLrc == "null" ? null : Regex.Unescape(strLrc.Substring(1, strLrc.Length - 2));
        }

        public static string GetTranslatedLyric(byte[] bytLyric)
        {
            int startIndex;
            int endIndex;
            int subStartIndex;
            string strLrc;

            startIndex = GetIndex(bytLyric, TLyric, 0);
            endIndex = GetIndex(bytLyric, LyricEnd, startIndex);
            subStartIndex = GetIndex(bytLyric, Lyric, startIndex, endIndex) + Lyric.Length;
            strLrc = Encoding.UTF8.GetString(bytLyric, subStartIndex, endIndex - subStartIndex);
            return strLrc == "null" ? null : Regex.Unescape(strLrc.Substring(1, strLrc.Length - 2));
        }

        private static int GetIndex(byte[] src, byte dest, int startIndex)
        {
            return GetIndex(src, dest, startIndex, src.Length - 1);
        }

        private static int GetIndex(byte[] src, byte dest, int startIndex, int endIndex)
        {
            for (int i = startIndex; i < endIndex + 1; i++)
                if (src[i] == dest)
                    return i;
            return -1;
        }

        private static int GetIndex(byte[] src, byte[] dest, int startIndex)
        {
            return GetIndex(src, dest, startIndex, src.Length - dest.Length);
        }

        private static int GetIndex(byte[] src, byte[] dest, int startIndex, int endIndex)
        {
            int j;

            for (int i = startIndex; i < endIndex + 1; i++)
                if (src[i] == dest[0])
                {
                    for (j = 1; j < dest.Length; j++)
                        if (src[i + j] != dest[j])
                            break;
                    if (j == dest.Length)
                        return i;
                }
            return -1;
        }

        private static byte[] GetWebSrc(string url)
        {
            Stream stream;
            byte[] buffer;
            List<byte> byteList;
            int count;

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)((HttpWebRequest)WebRequest.Create(url)).GetResponse())
                {
                    stream = response.GetResponseStream();
                    buffer = new byte[0x1000];
                    byteList = new List<byte>();
                    for (int i = 0; i < int.MaxValue; i++)
                    {
                        count = stream.Read(buffer, 0, buffer.Length);
                        if (count == 0x1000)
                            byteList.AddRange(buffer);
                        else if (count == 0)
                            return byteList.ToArray();
                        else
                            for (int j = 0; j < count; j++)
                                byteList.Add(buffer[j]);
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
            throw new OutOfMemoryException();
        }
    }
}
