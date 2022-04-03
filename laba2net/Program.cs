using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace FtpConsoleClient
{
    class Program
    {
        class FtpClient {
            private string host;
            private string uri;
            private string password;
            private string username;

            public bool Passive = true;
            public bool Binary = true;
            public bool EnableSsl = false;
            public bool Hash = false;
            public FtpClient(string host, string username, string password)
            {
                if (!host.Contains("ftp://"))
                    this.host = "ftp://" + host;
                else
                    this.host = host;
                uri = this.host;
                this.username = username;
                this.password = password;
            }
            public string combine(string path1, string path2)
            {
                if (path2[0] == '/')
                    return host + path2;
                else
                    return Path.Combine(path1, path2).Replace("\\", "/");
            }
            public FtpWebRequest createRequest(string method)
            {
                return createRequest(uri, method);
            }
            private FtpWebRequest createRequest(string uri, string method)
            {
                var r = (FtpWebRequest)WebRequest.Create(uri);
             
                r.Credentials = new NetworkCredential(username, password);
                r.Method = method;
                r.UseBinary = Binary;
                r.EnableSsl = EnableSsl;
                r.UsePassive = Passive;
                ServicePointManager.ServerCertificateValidationCallback =
                      (s, certificate, chain, sslPolicyErrors) => true;

                return r;
            }
            public void ChangeWorkingDirectory(string path)
            {
                uri = combine(uri, path);
            }
            public void GetPastDir()
            {
                string[] split = uri.Split("/");
                uri = uri.Replace("/" + split[split.Length - 1], "");
            }
            public string PrintWorkingDirectory()
            {
                return uri;
            }          
            public string[] ListDirectoryDetails()
            {
                var list = new List<string>();

                var request = createRequest(WebRequestMethods.Ftp.ListDirectoryDetails);
                var response = (FtpWebResponse)request.GetResponse();
                using (var stream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(stream, true))
                    {
                        while (!reader.EndOfStream)
                        {
                            list.Add(reader.ReadLine());
                        }
                    }
                }
                Console.WriteLine($"Статус: {response.StatusDescription}");
                return list.ToArray();
            }
            public void RemoveDir()
            {
                var list = new List<string>();
                var request = createRequest(WebRequestMethods.Ftp.ListDirectoryDetails);
                var response = (FtpWebResponse)request.GetResponse();
                Regex parser = new Regex(@"(?<dir>[\-dl])(?<permission>([\-r][\-w][\-xs]){3})\s+\d+\s+\w+\s+\w+\s+(?<size>\d+)\s+(?<timestamp>\w+\s+\d+\s+\d+.\d+)\s+(?<name>.+)");
                using (var stream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(stream, true))
                    {
                        while (!reader.EndOfStream)
                        {
                            list.Add(reader.ReadLine());
                        }
                        if (list.Count > 0)
                        {
                            foreach (string line in list)
                            {
                                Match match = parser.Match(line);
                                if (match != null)
                                {
                                    ChangeWorkingDirectory(match.Groups["name"].ToString());
                                    if (match.Groups["dir"].ToString() == "d")
                                    {
                                        RemoveDir();
                                    }
                                    else
                                    {
                                        request = createRequest(WebRequestMethods.Ftp.DeleteFile);
                                        response = (FtpWebResponse)request.GetResponse();
                                        if (response.StatusDescription.Contains("200"))
                                            Console.WriteLine($"File {uri} removed successfully.\nStatus: " + response.StatusDescription);
                                        else
                                            Console.WriteLine("Error.\nStatus: " + response.StatusDescription);
                                        response.Close();
                                    }
                                    uri = uri.Replace("/" + match.Groups["name"].ToString(), "");
                                }
                            }
                        }
                        request = createRequest(WebRequestMethods.Ftp.RemoveDirectory);
                        response = (FtpWebResponse)request.GetResponse();
                        if (response.StatusDescription.Contains("200"))
                            Console.WriteLine($"Dir {uri} removed successfully.\nStatus: " + response.StatusDescription);
                        else
                            Console.WriteLine("Error.\nStatus: " + response.StatusDescription);
                        response.Close();
                    }
                }
            }
            public void RemoveSizeGreater()
            {
                string pastUri = uri;
                string path;
                int size;
                Console.WriteLine("Enter size to delete dir: ");
                try
                {
                    int.TryParse(Console.ReadLine(), out size);
                    Console.WriteLine("Enter dir path: ");
                    path = Console.ReadLine();
                    ChangeWorkingDirectory(path);
                    if (GetDirSize() > size)
                    {
                        RemoveDir();
                    }
                    else
                        Console.WriteLine("Dir's size not greater");
                    if (pastUri.Contains(path))
                    {
                        uri = host + path;
                        GetPastDir();
                    }
                    else
                        uri = pastUri;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex);
                }

            }
            public long GetDirSize()
            {
                var list = new List<string>();
                long totalSize = 0;
                var request = createRequest(WebRequestMethods.Ftp.ListDirectoryDetails);
                var response = (FtpWebResponse)request.GetResponse();
                Regex parser = new Regex(@"(?<dir>[\-dl])(?<permission>([\-r][\-w][\-xs]){3})\s+\d+\s+\w+\s+\w+\s+(?<size>\d+)\s+(?<timestamp>\w+\s+\d+\s+\d+.\d+)\s+(?<name>.+)");
                using (var stream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(stream, true))
                    {
                        while (!reader.EndOfStream)
                        {
                            list.Add(reader.ReadLine());
                        }
                        if (list.Count > 0)
                        {
                            foreach (string line in list)
                            {
                                Match match = parser.Match(line);
                                if (match != null)
                                {
                                    long size = 0;
                                    if (match.Groups["dir"].ToString() == "d")
                                    {
                                        ChangeWorkingDirectory(match.Groups["name"].ToString());
                                        totalSize += GetDirSize();
                                        uri = uri.Replace("/" + match.Groups["name"].ToString(), "");
                                    }
                                    else if (long.TryParse(match.Groups["size"].Value, out size))
                                    {
                                        totalSize += size;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine(line);
                                }
                            }
                            return totalSize;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
            }
        }
        private static void PrintMenu()
        {
            Console.WriteLine("1) Change dir\n" +
                "2) Get size of dir\n" +
                "3) Delete dir if size greater\n" +
                "4) Go to past dir\n" +
                "5) Quit");
        }
        static void Main()
        {
            Console.WriteLine("Enter host name: ");
            string host = Console.ReadLine();
            //login in
            Console.WriteLine("Enter login: ");
            string username = Console.ReadLine();
            Console.WriteLine("Enter password: ");
            string password = Console.ReadLine();
            FtpClient client = new FtpClient(host, username, password);
            try
            {
                string choice = "";
                do
                {
                    Console.Clear();
                    Console.WriteLine("Current directory: " + client.PrintWorkingDirectory());
                    string[] list = client.ListDirectoryDetails();
                    for (int i = 0; i < list.Length; i++)
                        Console.WriteLine(list[i]);
                    PrintMenu();
                    choice = Console.ReadLine();
                    switch (choice)
                    {
                        case "1":
                            try
                            {
                                Console.WriteLine("Enter dir: ");
                                client.ChangeWorkingDirectory(Console.ReadLine());
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Exception: " + ex);
                            }
                            break;
                        case "2":
                            Console.WriteLine("Size of dir: " + client.GetDirSize());
                            break;
                        case "3":
                            client.RemoveSizeGreater();
                            break;
                        case "4":
                            client.GetPastDir();
                            break;
                    }
                    Console.ReadLine();
                } while (choice != "5");
                Console.WriteLine("Goodbye!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex);
            }
        }
    }
}