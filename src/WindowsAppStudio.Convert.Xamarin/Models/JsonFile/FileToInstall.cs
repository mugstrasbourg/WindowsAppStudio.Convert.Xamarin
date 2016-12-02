using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Newtonsoft.Json;


namespace WindowsAppStudio.Convert.Xamarin.Models.JsonFile
{
    public class FileToInstall
    {
        public InstallType Action { get; set; }
        [DefaultValue(false)]
        public bool DeleteSourceFile { get; set; }


        [JsonIgnore]
        public static string SourceDirectory { get; set; }
        [JsonIgnore]
        public static string TargetDirectory { get; set; }


        private string _sourceName;
        [JsonProperty("Name")]
        public string SourceName
        {
            get
            {
                return _sourceName;
            }
            set { _sourceName = value.SanitizeFile(); }
        }

        private string _targetName;
        [JsonProperty("NewName")]
        public string TargetName
        {
            get
            {
                return (string.IsNullOrEmpty(_targetName) ? SourceName : _targetName);
            }
            set { _targetName = value.SanitizeFile(); }
        }

        public string SourceFilename => Path.Combine(Action == InstallType.Update ? TargetDirectory : SourceDirectory, SourceName);
        public string TargetFilename => Path.Combine(TargetDirectory, TargetName);


        public Dictionary<string, string> _searchReplaceRegExDictionary;
        [JsonProperty("Regex")]
        public Dictionary<string, string> SearchReplaceRegExDictionary
        {
            get
            {
                return _searchReplaceRegExDictionary ?? (_searchReplaceRegExDictionary = new Dictionary<string, string>());
            }
            set { _searchReplaceRegExDictionary = value; }
        }

        public RegexOptions RegexOption { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool VerifySearchRegex { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool ShowError { get; set; }

        public void RenameInDictionary(string search, string replace, bool inSearch, bool inReplace)
        {
            if (!inSearch && !inReplace)
                return;
            SearchReplaceRegExDictionary = SearchReplaceRegExDictionary
                   .Select(x => new KeyValuePair<string, string>(inSearch ? x.Key.Replace(search, replace) : x.Key, inReplace ? x.Value.Replace(search, replace) : x.Value))
                   .ToDictionary(x => x.Key, x => x.Value);
        }

        public void Rename(string search, string replace)
        {
            TargetName = TargetName.Replace(search, replace);
            if (Action == InstallType.Update)
            {
                SourceName = SourceName.Replace(search, replace);
                RenameInDictionary(search, replace, true, true);
            }
            else
                SearchReplaceRegExDictionary.Add(search, replace);
        }

        public void Install()
        {
            if (!File.Exists(SourceFilename))
            {
                if (ShowError)
                    throw new Exception($"Error: \"{SourceFilename}\" not found.");
                else
                    return;
            }

            string modifiedText = File.ReadAllText(SourceFilename);
            if (!string.IsNullOrEmpty(modifiedText) && SearchReplaceRegExDictionary != null && SearchReplaceRegExDictionary.Count > 0)
                SearchReplaceRegExDictionary?.Keys.ToList().ForEach(searchString =>
                {
                    Regex regex = new Regex(searchString, RegexOption);
                    if (!VerifySearchRegex || regex.IsMatch(modifiedText))
                    {
                        modifiedText = regex.Replace(modifiedText, SearchReplaceRegExDictionary[searchString]);
                    }
                    else
                    {
                        throw new Exception($"Error: \"{searchString}\" not found.");
                    }
                });
            Directory.CreateDirectory(Path.GetDirectoryName(TargetFilename));
            File.WriteAllText(TargetFilename, modifiedText);
            if (DeleteSourceFile)
                File.Delete(SourceFilename);
        }

        public FileToInstall Clone()
        {
            return (FileToInstall)this.MemberwiseClone();
        }
    }
}
