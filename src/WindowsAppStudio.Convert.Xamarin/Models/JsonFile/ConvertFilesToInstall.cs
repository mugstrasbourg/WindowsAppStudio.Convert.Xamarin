using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsAppStudio.Convert.Xamarin.Models.JsonFile
{
    class ConvertFilesToInstall
    {

        private string _zipBaseFile;
        public string ZipBaseFile
        {
            get { return _zipBaseFile; }
            set { _zipBaseFile = value.SanitizeFile(); }
        }

        public List<FileToInstall> BaseFiles { get; set; }
        public List<FileToInstall> SectionFiles { get; set; }
    }
}
