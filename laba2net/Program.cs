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
                this.host = host;
                uri = "ftp://" + host;
                this.username = username;
                this.password = password;
            }
            public string combine(string path1, string path2)
            {
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
        static void Main()
        {
            Console.WriteLine("Введите адрес хоста: ");
            string host = Console.ReadLine();
            //login in
            Console.WriteLine("Введите логин: ");
            string username = Console.ReadLine();
            Console.WriteLine("Введите пароль: ");
            string password = Console.ReadLine();
            FtpClient client = new FtpClient(host, username, password);

            client.ChangeWorkingDirectory("Examples");
            Console.WriteLine(client.GetDirSize());
            string[] list = client.ListDirectoryDetails();
            for (int i = 0; i < list.Length; i++)
                Console.WriteLine(list[i]);
            /*client.ChangeWorkingDirectory("Data");
            list = client.ListDirectoryDetails();
            for (int i = 0; i < list.Length; i++)
                Console.WriteLine(list[i]);*/
            Console.WriteLine("Current directory: " + client.PrintWorkingDirectory());
        }
    }
}