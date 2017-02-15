using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OneTrueError.Client.SysCore.Contracts
{
    public class UploadToScreenshotsDTO
    {
        public string Name { get; set; }
        public string ErrorId { get; set; }
        public string ImageBase64 { get; set; }
    }
}
