using System;
using System.IO;
using System.Net;
using System.Text;

namespace FtpConsoleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            // Создаем объект FtpWebRequest
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://localhost:21/Data/");
            // устанавливаем метод на загрузку файлов
            request.Method = "PWD";
                //WebRequestMethods.Ftp.PrintWorkingDirectory;
            // login in
            request.Credentials = new NetworkCredential("anonymous", "123");
            request.EnableSsl = true; // если используется ssl
            ServicePointManager.ServerCertificateValidationCallback =
                      (s, certificate, chain, sslPolicyErrors) => true;

            // получаем ответ от сервера в виде объекта FtpWebResponse
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            // получаем поток ответа
            Stream responseStream = response.GetResponseStream();
            // сохраняем файл в дисковой системе
            // создаем поток для сохранения файла
            /*FileStream fs = new FileStream("F:/newTest.txt", FileMode.Create);

            //Буфер для считываемых данных
            byte[] buffer = new byte[64];
            int size = 0;
            while ((size = responseStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                fs.Write(buffer, 0, size);
            }
            fs.Close();*/
            byte[] buffer = new byte[64];
            int size = responseStream.Read(buffer, 0, buffer.Length);
            Console.WriteLine("Working directory: " + Encoding.UTF8.GetString(buffer));
            response.Close();

            request.Method = WebRequestMethods.Ftp.ListDirectory;
            response = (FtpWebResponse)request.GetResponse();
            responseStream = response.GetResponseStream();

            Console.WriteLine("Files in directory: " + responseStream.ToString());

            response.Close();

            Console.WriteLine("Загрузка и сохранение файла завершены");
            Console.Read();
        }
    }
}