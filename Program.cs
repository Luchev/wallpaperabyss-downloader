using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace WallpaperDownloader
{
    class Program
    {
        static string input = "";
        static string url = "";
        static string defaultUrl = "https://wall.alphacoders.com/by_category.php?id=3";
        static string tempInput = "";
        static int pages = 0;
        static int startFromPage = 1;
        static string saveFile = "pictures.dat";
        static bool running = false;
        static string wallName = "";
        static int fileCounter = 0;
        static int failedCounter = 0;
        static string saveDir = "D:/Pictures/Download/";
        static Regex rgxMain = new Regex(@"<img\ssrc=""[\w\W]*?<\/span>");
        //static Regex rgxName = new Regex(@"\d+\.\w+(?="")");
        static Regex rgxName = new Regex(@"(?<=<img\ssrc="").+?(?=""\s)");
        static Regex rgxRes = new Regex(@"\d+x\d+(?=<\/span)");
        static char[] digits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            try
            {
                Console.WriteLine("Wallpaper donwloader started");
                while (input != "exit")
                {
                    Console.Write("~>");
                    input = Console.ReadLine();
                    processInput(input);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Check errors.log for more info");
                File.AppendAllText("errors.log", Environment.NewLine + "ERROR:" + "\tat " + DateTime.Now.ToString("h:mm:ss tt") + Environment.NewLine + e.ToString() + Environment.NewLine);
            }
            finally
            {

            }
        }

        static void processInput(string input)
        {
            try
            {
                switch (input)
                {
                    case "h":
                    case "help":
                        Console.WriteLine("Help menu");
                        Console.WriteLine("h / help \t-\t Bring the help menu.");
                        Console.WriteLine("exit \t-\t Exit the program.");
                        Console.WriteLine("e \t-\t Extract wallpaper names from url.");
                        Console.WriteLine("d \t-\t Download all wallpaper from previously extracted file.");
                        break;

                    case "exit":
                        break;

                    case "e":
                        Console.Write("Url (leave empty for default):");
                        url = Console.ReadLine().Trim().Trim(digits);
                        if (url == "")
                            url = defaultUrl;
                        
                        Console.Write("Number of pages (default 1):");
                        tempInput = Console.ReadLine();
                        if (tempInput == "" || tempInput == "1")
                        {
                            pages = 1;
                            startFromPage = 1;
                        }
                        else
                        {
                            if (Int32.TryParse(tempInput, out pages))
                                if (url.IndexOf("&page=") < 1)
                                    url += "&page=";
                            if (pages == 0)
                                pages = 1;
                        }

                        if (pages != 1)
                        {
                            Console.Write("Page to start from (default 1):");
                            tempInput = Console.ReadLine();
                            if (tempInput != "" || tempInput != "1")
                                int.TryParse(tempInput, out startFromPage);
                            if (startFromPage == 0 || pages <= startFromPage)
                                startFromPage = 1;

                        }
                        
                        Console.Write("File name (default pictures.dat):");
                        tempInput = Console.ReadLine();
                        if (tempInput != "")
                            saveFile = tempInput;

                        // Confirmation
                        Console.WriteLine("\nExtracting from url {0}.\nExtracting {1} pages starting from page {2}.\nFile will be saved in {3}{4}\n", url, pages, startFromPage.ToString().ToString(), Environment.CurrentDirectory + "\\", saveFile);

                        running = false;
                        if (pages == 1)
                        {
                            processHTML(url, saveFile, 0);
                        }
                        else
                        {
                            for (int i = startFromPage; i < pages + 1; i++)
                            {
                                processHTML(url, saveFile, i);
                            }
                        }
                        consoleUpdate("Complete", "Extracted", 3, pages);
                        fileCounter = 0;
                        break;

                    case "d":
                        Console.Write("File name (default pictures.dat):");
                        tempInput = Console.ReadLine().Trim();
                        if (tempInput == "")
                            saveFile = "pictures.dat";
                        else
                            saveFile = tempInput;
                        
                        Console.Write("Directory to save wallpapers (default D:/Pictures/Download/):");
                        tempInput = Console.ReadLine();
                        if (tempInput == "")
                            saveDir = "D:/Pictures/Download/";
                        else
                            saveDir = tempInput;

                        int minWidth;
                        Console.Write("Minimun width (default all):");
                        tempInput = Console.ReadLine().Trim();
                        if (tempInput == "")
                            minWidth = 0;
                        else
                            Int32.TryParse(tempInput, out minWidth);

                        int minHeight;
                        Console.Write("Minimun height (default all):");
                        tempInput = Console.ReadLine().Trim();
                        if (tempInput == "")
                            minHeight = 0;
                        else
                            Int32.TryParse(tempInput, out minHeight);

                        Console.WriteLine();
                        running = false;
                        downloadWalls(saveFile, minWidth, minHeight);
                        consoleUpdate("Complete", "Downloaded", 2, 0, false);
                        break;



                    default:
                        Console.WriteLine("Invalid input, type help or h to see commands.");
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Check errors.log for more info");
                File.AppendAllText("errors.log", Environment.NewLine + "Input error:" + "\tat " + DateTime.Now.ToString("h:mm:ss tt") + Environment.NewLine + e.ToString() + Environment.NewLine);
            }
        }


        static void processHTML(string _url, string _saveFile, int _page = 0)
        {
            try
            {
                if (_page != 0)
                    _url += _page.ToString();
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_url);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Stream received = response.GetResponseStream();
                    StreamReader reader = new StreamReader(received, Encoding.UTF8);
                    string data = reader.ReadToEnd();

                    foreach (Match match in rgxMain.Matches(data))
                    {
                        try
                        {
                            wallName = rgxName.Match(match.Value).Value.Replace("thumb-350-", "");
                            string res = rgxRes.Match(match.Value).Value;
                            if (wallName.Trim() != "" && res.Trim() != "")
                            {
                                File.AppendAllText(_saveFile, wallName + " - " + res + Environment.NewLine);
                                fileCounter++;
                            }

                            // OLD
                            //matchClean = match.Value.Replace("thumb-350-", "");
                            //string fileName = matchClean.Substring(matchClean.LastIndexOf("/") + 1);
                            //File.AppendAllText(_saveFile, fileName+ Environment.NewLine);

                        }
                        catch (Exception e)
                        {
                            File.AppendAllText("failed.log", wallName + "\t" + DateTime.Now.ToString("h:mm:ss tt") + Environment.NewLine);
                            File.AppendAllText("errors.log", Environment.NewLine + "Input error:" + "\tat " + DateTime.Now.ToString("h:mm:ss tt") + Environment.NewLine + e.ToString() + Environment.NewLine);
                            failedCounter++;
                        }
                        finally
                        {
                            consoleUpdate("Running", "Extracted", 3, _page);
                        }
                    }
                    response.Close();
                    reader.Close();
                }
                else
                {
                    throw new Exception("Web page didn't respond correctly.");
                }
            }
            catch (Exception e)
            {
                File.AppendAllText("errors.log", e.Message + Environment.NewLine);
            }
        }

        static void downloadWalls(string _fileName, int minWidth = 0, int minHeight = 0)
        {
            try
            {
                Console.WriteLine("Wallpapers to go through: {0}", File.ReadAllLines(_fileName).Length);
                while (File.ReadAllLines(_fileName).Length > 0)
                {
                    try
                    {
                        var lastLine = File.ReadLines(_fileName).Last();
                        var split = lastLine.Split(new string[] { " - " }, StringSplitOptions.None);
                        url = split[0];
                        int width;
                        int height;
                        Int32.TryParse(split[1].Split('x')[0], out width);
                        Int32.TryParse(split[1].Split('x')[1], out height);

                        if ((minWidth == 0 && minHeight == 0) || minWidth < width || minHeight < height)
                        {
                            using (var client = new WebClient())
                            {
                                string tempName = url.Substring(url.LastIndexOf("/") + 1);
                                client.DownloadFile(url, Path.Combine(saveDir + tempName));
                            };
                        }

                        fileCounter++;
                        string[] lines = File.ReadAllLines(_fileName);
                        Array.Resize(ref lines, lines.Length - 1);
                        File.WriteAllLines(_fileName, lines);

                        consoleUpdate("Running", "Downloaded", 2, 0, false);
                    }
                    catch (Exception e)
                    {
                        File.AppendAllText("failed.log", url);
                        File.AppendAllText("errors.log", Environment.NewLine + "Input error:" + "\tat " + DateTime.Now.ToString("h:mm:ss tt") + Environment.NewLine + e.ToString() + Environment.NewLine);
                        failedCounter++;
                    }
                }

            }
            catch (Exception e)
            {
                File.AppendAllText("errors.log", Environment.NewLine + "Input error:" + "\tat " + DateTime.Now.ToString("h:mm:ss tt") + Environment.NewLine + e.ToString() + Environment.NewLine);
            }
    }

        static void consoleUpdate(string status = "Running", string info = "Extracted", int linesAbove = 3, int _page = 0, bool _showPage = true)
        {
            if (running)
            {
                Console.SetCursorPosition(0, Console.CursorTop - linesAbove);
            }
            else
            {
                running = true;
            }

            Console.Write(new String(' ', Console.BufferWidth - 1));
            Console.WriteLine("\rStatus: {0}", status);
            if (_showPage)
                Console.WriteLine("Page {0}/{1}", _page.ToString(), pages);

            Console.WriteLine("{0} {1} of which {2} failed",info, fileCounter, failedCounter);
            
        }
    }
}