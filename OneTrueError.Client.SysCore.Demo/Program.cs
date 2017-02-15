using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneTrueError.Client.SysCore.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var url = new Uri("http://localhost:50473/");
            OneTrueProxy.Credentials(url, "5fb563d2b41144c0996b768064bdc5d1", "de7ff5583e8945ddb781c1f0662a2966");
            try
            {

                throw new InvalidOperationException("ReportGenerate OneTrueError.Client.SysCore.Demo");
            }
            catch (Exception ex)
            {
                var dto = OneTrueProxy.GenerateUploadReport(ex);
                UploadScreenshots(dto.ReportId);
            }
        }

        private static void UploadScreenshots(string errorId)
        {
            var reader = File.ReadAllBytes(@"E:\Local\OneTrueError\OneTrueError.SysCore\OneTrueError.Client.SysCore.Demo\Nuget\screenshots.jpg");
            OneTrueProxy.UploadScreenshots(errorId, "SysCoreLib", Convert.ToBase64String(reader));
        }
    }
}
