using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Security.Cryptography;

namespace FlatFolder
{
    class Program
    {
        public static Random ran = new Random();

        public static string ProcessPath = string.Empty;
        public static bool IsExtract = false;
        public static string logFilename = "Flatlog.nfo";
        public static string logFile = string.Empty;

        public static List<FileItem> FileItems=new List<FileItem> ();
        public static string SepChar = Path.DirectorySeparatorChar.ToString();
        public static MD5 md5 = new MD5CryptoServiceProvider();

        static void Main(string[] args)
        {
            

            Console.ResetColor();
            SetColor();

            #if DEBUG
            string tPath = @"D:\PS\DEV\FlatFolder\test\3\12.3 mark";
                args = new string[2] { "-e", tPath };
            #endif

            if (IsArgsValid(args))
            {
                //Console.WriteLine("args valid!");
                if (!ProcessPath.EndsWith(SepChar))
                { ProcessPath = ProcessPath + SepChar; }
                logFile = ProcessPath + logFilename;

                if (IsExtract)
                { Extract(); }
                else
                { Restore(); }
            }
            else
            {
                Console.WriteLine("Invalid arguments!\r\n");
                ShowUsage();
            }
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            Console.ResetColor();
        }

        #region ShowUsage
        /// <summary>
        /// ShowUsage
        /// </summary>
        static void ShowUsage()
        {
            Console.Write("FlatFolder -e[xtract]|-r[estore] Path");
            Console.WriteLine("\r\n");
        }
        #endregion

        #region Validate args
        /// <summary>
        /// If args are valid
        /// </summary>
        /// <param name="Args"></param>
        /// <returns></returns>
        static bool IsArgsValid(string[] Args)
        {
            if (Args.Length == 1)
            {
                ProcessPath = System.Environment.CurrentDirectory;
                return IsArgsFirstValid(Args[0]);
            }
            else if (Args.Length == 2)
            {
                if (IsArgsFirstValid(Args[0]))
                {
                    ProcessPath = Args[1];
                    return Directory.Exists(Args[1]);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        static bool IsArgsFirstValid(string ArgsFirst)
        {
            if (ArgsFirst.ToLower() == "-e" || ArgsFirst.ToLower() == "-extract")
            {
                IsExtract = true;
                return true;
            }
            else if (ArgsFirst.ToLower() == "-r" || ArgsFirst.ToLower() == "-restore")
            {
                IsExtract = false;
                return true;
            }
            else
            {
                return false;
            }

        }
        #endregion

        #region Color Change
        public static void SetColor()
        {
            //Console.BackgroundColor = RandomColor();
            Console.ForegroundColor = RandomColor();
            while (Console.ForegroundColor == ConsoleColor.Black || Console.ForegroundColor == ConsoleColor.DarkBlue)
            {
                Console.ForegroundColor = RandomColor();
            }
        }

        public static ConsoleColor RandomColor()
        {
            //Return random ColsoleColor between 0 to 15
            return (ConsoleColor)ran.Next(0, 15);
        }
        #endregion

        static void ReadFiles(string[] files, ref long count)
        {
            foreach (string file in files)
            {
                FileItem fileItem = new FileItem();
                fileItem.original = Path.GetFileName(file);
                fileItem.newName = getNewFilename(file);

                string filePath = Path.GetDirectoryName(file);
                if (!filePath.EndsWith(SepChar))
                { filePath = filePath + SepChar; }

                if (ProcessPath == filePath)
                {
                    fileItem.path = "";
                }
                else
                {
                    fileItem.path = Path.GetDirectoryName(file).Substring(ProcessPath.Length, Path.GetDirectoryName(file).Length - ProcessPath.Length);
                }
                if (fileItem.path != "")
                {
                    if (!fileItem.path.EndsWith(SepChar))
                    { fileItem.path = fileItem.path + SepChar; }
                }
                try
                {
                    File.Copy(file, ProcessPath + fileItem.newName);
                    count++;
                    Console.Write(".");
                }
                catch (IOException ioe)
                {
                    Console.WriteLine("Program Error!");
                }
                FileItems.Add(fileItem);
            }
        }

        static void Extract()
        {
            Console.WriteLine("Extracting please wait...");
            long count = 0;

            string[] topFiles = Directory.GetFiles(ProcessPath, "*.*", SearchOption.TopDirectoryOnly);
            ReadFiles(topFiles, ref count);

            string[] childDirectories = Directory.GetDirectories(ProcessPath, "*.*", SearchOption.TopDirectoryOnly);
            foreach (string childDirectory in childDirectories)
            {
                string[] files = Directory.GetFiles(childDirectory, "*.*", SearchOption.AllDirectories);
                ReadFiles(files, ref count);
            }
            Console.WriteLine("\r\nExtract finished");
            Console.WriteLine(string.Format("Totally extracted {0} file(s).",count));
            WriteLog();
            if (File.Exists(logFile))
            {
                File.SetAttributes(logFile, FileAttributes.ReadOnly | FileAttributes.Hidden);
            }
        }

        static string getNewFilename(string Filename)
        {
            string returnFileName = string.Empty;
            byte[] bfilename = Encoding.UTF8.GetBytes(Filename.Trim());
            byte[] rfilename = md5.ComputeHash(bfilename);
            returnFileName = BitConverter.ToString(rfilename).Replace("-", "") + "-" + Path.GetFileName(Filename);

            return returnFileName;
        }

        static void WriteLog()
        {
            if (File.Exists(logFile))
            {
                ConsoleColor temp = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(string.Format("{0} already exist, press Enter to overwrite it!",logFilename));
                Console.ForegroundColor = temp;
                Console.ReadLine();
                File.SetAttributes(logFile, FileAttributes.Normal);
                File.Delete(logFile);
            }
            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(logFile, false, Encoding.UTF8);
                XmlDocument doc = new XmlDocument();

                XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
                doc.AppendChild(dec);
                
                XmlNode rootNode=doc.CreateElement("log");
                XmlNode filesNode = doc.CreateElement("files");

                foreach (FileItem fi in FileItems)
                {
                    XmlNode fileNode = doc.CreateElement("file");

                    XmlNode originalNode = doc.CreateElement("original");
                    originalNode.InnerText = fi.original;
                    XmlNode newNode = doc.CreateElement("new");
                    newNode.InnerText = fi.newName;
                    XmlNode pathNode = doc.CreateElement("path");
                    pathNode.InnerText = fi.path;

                    fileNode.AppendChild(originalNode);
                    fileNode.AppendChild(newNode);
                    fileNode.AppendChild(pathNode);

                    filesNode.AppendChild(fileNode);
                }

                rootNode.AppendChild(filesNode);
                doc.AppendChild(rootNode);

                doc.Save(sw);
                sw.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Error when create {0}",logFilename));
            }
            finally
            {
                if (sw != null)
                { sw.Close(); }
            }
        }

        static void Restore()
        {
            Console.WriteLine("Extracting please wait...");
            long count = 0;

            ReadLog();
            foreach (FileItem fileItem in FileItems)
            {
                try
                {
                    if (!Directory.Exists(Path.GetDirectoryName(ProcessPath + fileItem.path + fileItem.original)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(ProcessPath + fileItem.path + fileItem.original));
                    }
                    File.Copy(ProcessPath + fileItem.newName, ProcessPath + fileItem.path + fileItem.original,true);
                    count++;
                    Console.Write(".");
                }
                catch (IOException ioe)
                {
                    Console.WriteLine("Program Error! IO Exception!");
                    Console.WriteLine(fileItem.original + "::" + ioe.Message);
                }
            }

            Console.WriteLine("\r\nExtract finished");
            Console.WriteLine(string.Format("Totally restored {0} file(s).", count));
        }

        static void ReadLog()
        {
            if (File.Exists(logFile))
            {
                StreamReader sr = null;
                try
                {
                    sr = new StreamReader(logFile, Encoding.UTF8, true);
                    XmlDocument doc = new XmlDocument();
                    doc.Load(sr);
                    XmlNodeList filesNodeList = doc.SelectNodes("./log/files/file");
                    foreach (XmlNode fileNode in filesNodeList)
                    {
                        FileItem fi = new FileItem();
                        fi.original = fileNode.SelectSingleNode("original").InnerText;
                        fi.newName = fileNode.SelectSingleNode("new").InnerText;
                        fi.path = fileNode.SelectSingleNode("path").InnerText;
                        FileItems.Add(fi);
                    }
                    doc = null;
                    sr.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(string.Format("Error when read {0}", logFilename));
                }
                finally
                {
                    if (sr != null)
                    { sr.Close(); }
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(string.Format("{0} not found, can not restore!", logFilename));
            }
        }
    }

    public class FileItem
    {
        public string original { set; get; }
        public string newName { set; get; }
        public string path { set; get; }
    }
}
