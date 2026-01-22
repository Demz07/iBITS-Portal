using System.Collections.Generic;

namespace iBITS_Portal.ViewModels
{
    public class ImportPreviewViewModel
    {
        public List<string> Headers { get; set; } = new List<string>();
        public List<List<string>> PreviewRows { get; set; } = new List<List<string>>();
    }
}