using System;
using System.IO;
using System.Text;

namespace 网易云音乐歌词下载器
{
    internal static class Program
    {
        private static Encoding _Encoding;

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        private static void Main()
        {
            string arg;
            string floder;
            int lyricMode;
            string extension;
            bool isMp3;
            string fileName;
            string lrcFile;
            byte[] byt163Key;
            string musicId;
            byte[] bytLyric;
            bool hasLyric;
            string lrc;
            string tlrc;

            Console.Title = "网易云音乐歌词下载器";
#if DEBUG
            floder = @"G:\Music";
            _Encoding = Encoding.UTF8;
            lyricMode = 1;
#else
            Console.WriteLine("输入要同步歌词的文件夹：");
            inputFloder:
            floder = Console.ReadLine();
            if (!Directory.Exists(floder))
            {
                Console.WriteLine("文件夹不存在，请重新输入：");
                goto inputFloder;
            }
            Console.WriteLine("使用指定编码保存歌词文件（留空使用默认编码UTF-8）：");
            inputEncoding:
            arg = Console.ReadLine();
            if (arg == string.Empty)
                _Encoding = Encoding.UTF8;
            else
                try
                {
                    _Encoding = Encoding.GetEncoding(arg);
                }
                catch (Exception)
                {
                    Console.WriteLine("编码不存在，请重新输入：");
                    goto inputEncoding;
                }
            Console.WriteLine("是否使用原始歌词（输入“是”优先使用原始歌词，输入“否”优先使用翻译歌词，留空将逐一选择）：");
            inputLyricMode:
            arg = Console.ReadLine();
            if (arg == string.Empty)
                lyricMode = 0;
            else if (arg == "是")
                lyricMode = 1;
            else if (arg == "否")
                lyricMode = 2;
            else
            {
                Console.WriteLine("输入有误，请重新输入：");
                goto inputLyricMode;
            }
#endif
            foreach (string file in Directory.GetFiles(floder))
            {
                extension = Path.GetExtension(file).ToUpperInvariant();
                if (extension == ".MP3")
                    isMp3 = true;
                else if (extension == ".FLAC")
                    isMp3 = false;
                else
                    continue;
                fileName = Path.GetFileName(file);
                lrcFile = Path.ChangeExtension(file, ".lrc");
                if (File.Exists(lrcFile))
                {
                    Console.WriteLine(fileName + "的歌词文件已存在");
                    continue;
                }
                byt163Key = CloudMusicLyric.Get163Key(file, isMp3);
                if (byt163Key == null)
                {
                    Console.WriteLine("无法获取文件" + fileName + "的163Key，请手动输入歌曲ID下载歌词（留空将跳过此歌曲）：");
                    inputMusicId:
                    musicId = Console.ReadLine();
                    if (musicId == string.Empty)
                        continue;
                    else
                    {
                        if (int.TryParse(musicId, out int num))
                            goto getBytLyric;
                        else
                        {
                            Console.WriteLine("输入有误，请重新输入：");
                            goto inputMusicId;
                        }
                    }
                }
                musicId = CloudMusicLyric.GetMusicId(byt163Key);
                getBytLyric:
                bytLyric = CloudMusicLyric.GetLyric(musicId);
                if (!CloudMusicLyric.IsCollected(bytLyric))
                {
                    Console.WriteLine(fileName + "的歌曲ID未被收录，请手动输入歌曲ID下载歌词（留空将跳过此歌曲）：");
                    inputMusicId:
                    musicId = Console.ReadLine();
                    if (musicId == string.Empty)
                        continue;
                    else
                    {
                        if (int.TryParse(musicId, out int num))
                            goto getBytLyric;
                        else
                        {
                            Console.WriteLine("输入有误，请重新输入：");
                            goto inputMusicId;
                        }
                    }
                }
                hasLyric = CloudMusicLyric.HasLyric(bytLyric);
                if (hasLyric)
                {
                    lrc = CloudMusicLyric.GetOriginalLyric(bytLyric);
                    tlrc = CloudMusicLyric.GetTranslatedLyric(bytLyric);
                    if (lrc == null)
                    {
                        File.WriteAllText(lrcFile, tlrc, _Encoding);
                        Console.WriteLine(Path.ChangeExtension(fileName, ".lrc") + "下载成功，使用翻译歌词");
                    }
                    else if (tlrc == null)
                    {
                        File.WriteAllText(lrcFile, lrc, _Encoding);
                        Console.WriteLine(Path.ChangeExtension(fileName, ".lrc") + "下载成功，使用原始歌词");
                    }
                    else
                    {
                        switch (lyricMode)
                        {
                            case 0:
                                Console.WriteLine(fileName + "是否使用原始歌词（输入“是”使用原始歌词，输入“否”使用翻译歌词）：");
                                inputIfUsingOriginal:
                                arg = Console.ReadLine();
                                if (arg == "是")
                                {
                                    File.WriteAllText(lrcFile, lrc, _Encoding);
                                    Console.WriteLine(Path.ChangeExtension(fileName, ".lrc") + "下载成功，使用原始歌词");
                                }
                                else if (arg == "否")
                                {
                                    File.WriteAllText(lrcFile, tlrc, _Encoding);
                                    Console.WriteLine(Path.ChangeExtension(fileName, ".lrc") + "下载成功，使用翻译歌词");
                                }
                                else
                                {
                                    Console.WriteLine("输入有误，请重新输入：");
                                    goto inputIfUsingOriginal;
                                }
                                break;
                            case 1:
                                File.WriteAllText(lrcFile, lrc, _Encoding);
                                Console.WriteLine(Path.ChangeExtension(fileName, ".lrc") + "下载成功，使用原始歌词");
                                break;
                            case 2:
                                File.WriteAllText(lrcFile, tlrc, _Encoding);
                                Console.WriteLine(Path.ChangeExtension(fileName, ".lrc") + "下载成功，使用翻译歌词");
                                break;
                        }
                    }
                }
                else
                    Console.WriteLine(fileName + "无歌词文件");
            }
            Console.WriteLine("下载完成，按任意键退出");
            Console.ReadKey();
        }
    }
}
