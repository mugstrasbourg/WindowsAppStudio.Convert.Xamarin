using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PowerArgs;

namespace WindowsAppStudio.Convert.Xamarin.Models.Args
{
    public class ConvertSettings
    {


        private string _sourcePath;
        [ArgDefaultValue("")]
        [ArgShortcut("S")]
        [ArgDescription("Source path for the files to copy")]
        public string SourcePath
        {
            get
            {
                return _sourcePath;
            }
            set
            {
                _sourcePath = value?.SanitizePath();
            }
        }

        private string _appTargetPath;
        [ArgRequired(PromptIfMissing = true)]
        [ArgDescription("Target path for the Xamarin solution")]
        [ArgShortcut("T")]
        public string SlnTargetPath
        {
            get
            {
                return _appTargetPath;
            }
            set
            {
                _appTargetPath = value?.SanitizePath();
            }
        }


        [ArgDescription("NameSpace of the AppStudio app")]
        [ArgShortcut("N")]
        public string NameSpace { get; set; }


        [ArgDescription("List of the collections (in fact, the name of the 'WasAppSectionNameSection.cs' files")]
        [ArgShortcut("L")]
        public string[] Collections { get; set; }


        [ArgDefaultValue(true)]
        [ArgDescription("Convert also the base files (or just the sections files)")]
        [ArgShortcut("B")]
        public bool WithBase { get; set; }

        [ArgDefaultValue(true)]
        [ArgDescription("Convert also the collections files (= sections files)")]
        [ArgShortcut("C")]
        public bool WithCollections { get; set; }

        public string GetWin10Path(string path)
        {
            return Path.Combine(path, NameSpace + ConvertResources.AppStudioPathExtension);
        }
        public string GetXamarinPath(string path)
        {
            return Path.Combine(path, NameSpace + ConvertResources.XamarinPathExtension);
        }
    }
}
