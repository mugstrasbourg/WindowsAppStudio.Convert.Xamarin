using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WindowsAppStudio.Convert.Xamarin.Models.Args;
using WindowsAppStudio.Convert.Xamarin.Models.JsonFile;
using Newtonsoft.Json;
using PowerArgs;


namespace WindowsAppStudio.Convert.Xamarin
{
    class Program
    {
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs, bool overwrite)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, overwrite);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs, overwrite);
                }
            }
        }

        static void Main(string[] args)
        {
            try
            {
                var settings = Args.Parse<ConvertSettings>(args);


                if (!settings.WithBase && !settings.WithCollections)
                    throw new Exception(ConvertResources.ErrorNothingToDo);

                #region Search the Windows App Sudio source folder (ie. "C:\dev\My_WAS_Appli\MyAppliNamespace.W10")
                if (string.IsNullOrEmpty(settings.NameSpace))
                {
                    try
                    {
                        var appStudioPath =
                            Directory.EnumerateDirectories(settings.SlnTargetPath)
                            .Select(Path.GetFileName)
                            .First(fileName => fileName.EndsWith(ConvertResources.AppStudioPathExtension));
                        settings.NameSpace = Path.GetFileNameWithoutExtension(appStudioPath);
                    }
                    catch (Exception)
                    {
                        throw new Exception(ConvertResources.ErrorFindingApp);
                    }
                }
                else
                {
                    if (!Directory.Exists(settings.GetWin10Path(settings.SlnTargetPath)))
                        throw new Exception(ConvertResources.ErrorFindingApp);
                }
                #endregion

                #region The Xamarin solution folder must not exist
                if (Directory.Exists(settings.GetXamarinPath(settings.SlnTargetPath)))
                    throw new Exception(ConvertResources.ErrorXamarinFolderAlreadyExists);
                #endregion

                #region Search the sections names
                if (settings.WithCollections && (settings.Collections == null || settings.Collections.Length == 0))
                {
                    try
                    {
                        settings.Collections = Directory.EnumerateFiles(Path.Combine(settings.GetWin10Path(settings.SlnTargetPath), ConvertResources.AppStudioSectionsDirectory))
                            .Select(Path.GetFileNameWithoutExtension)
                            .Where(fileName => fileName.EndsWith(ConvertResources.AppStudioSectionEndingFilename) && fileName != ConvertResources.AppStudioSectionFileIgnore)
                            .Select(fileName => fileName.Substring(0, fileName.Length - ConvertResources.AppStudioSectionEndingFilename.Length))
                            .ToArray();
                    }
                    catch (Exception)
                    {
                        throw new Exception(ConvertResources.ErrorFindingSections);
                    }
                }
                #endregion

                Console.WriteLine(InstallFiles(settings));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ArgUsage.GenerateUsageFromTemplate<ConvertSettings>());
            }
        }

        static string InstallFiles(ConvertSettings settings)
        {
            string[] txtExtensions = { ".txt", ".xaml", ".cs", ".xml", ".csproj", ".sln", ".config", ".sh", ".xlf", ".appxmanifest" };
            string result = "";
            var dictProjectsGuid = new Dictionary<string, string>();

            FileToInstall.SourceDirectory = settings.SourcePath;
            FileToInstall.TargetDirectory = settings.GetXamarinPath(settings.SlnTargetPath);

            ConvertFilesToInstall convertJson = new ConvertFilesToInstall();
            convertJson = JsonConvert.DeserializeObject<ConvertFilesToInstall>(File.ReadAllText(Path.Combine(settings.SourcePath, "ConvertFiles.json")));

            if (settings.WithBase)
            {
                #region Unzip the zip file

                if (!string.IsNullOrEmpty(convertJson.ZipBaseFile))
                {
                    MemoryStream saveStream = new MemoryStream();
                    using (var newZipArchive = new ZipArchive(saveStream, ZipArchiveMode.Update, true))
                    {
                        ZipArchive zipArchive =
                            ZipFile.OpenRead(Path.Combine(settings.SourcePath, convertJson.ZipBaseFile));
                        foreach (var entry in zipArchive.Entries)
                        {
                            if (Path.GetExtension(entry.Name) == ".sln")
                            {
                                using (var streamZip = entry.Open())
                                using (var entryStream = new MemoryStream())
                                {
                                    streamZip.CopyTo(entryStream);
                                    string entryString = Encoding.UTF8.GetString(entryStream.ToArray());
                                    var projectGuids = new Regex(
                                            @"\{([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\}""[\r\n \t]*EndProject")
                                            .Matches(entryString);
                                    foreach (Match projectGuid in projectGuids)
                                    {
                                        var textGuid = projectGuid.Groups[1].Value;
                                        if (!dictProjectsGuid.ContainsKey(textGuid))
                                            dictProjectsGuid.Add(textGuid, Guid.NewGuid().ToString().ToUpper());
                                    }
                                }
                            }
                        }
                        foreach (var zipArchiveEntry in zipArchive.Entries)
                        {

                            var newZipArchiveEntry =
                                newZipArchive.CreateEntry(zipArchiveEntry.FullName.Replace("WasAppNamespace",
                                    settings.NameSpace));

                            using (var streamZip = zipArchiveEntry.Open())
                            using (var newStreamZip = newZipArchiveEntry.Open())
                            {
                                var entryExtension = Path.GetExtension(newZipArchiveEntry.Name);
                                // Replace just the name of the files, if necessary ("" or extension of the list)
                                if (!txtExtensions.Contains(entryExtension))
                                    streamZip.CopyTo(newStreamZip);
                                //  Replace the name of the files AND the content of the files, if necessary
                                else
                                    using (var entryStream = new MemoryStream())
                                    {
                                        streamZip.CopyTo(entryStream);
                                        string entryModified =
                                            Encoding.UTF8.GetString(entryStream.ToArray())
                                                .Replace("WasAppNamespace", settings.NameSpace);

                                        foreach (var keyValuePair in dictProjectsGuid)
                                        {
                                            entryModified = Regex.Replace(entryModified, keyValuePair.Key, keyValuePair.Value, RegexOptions.IgnoreCase);
                                        }

                                        byte[] byteArrayEntryStream = Encoding.UTF8.GetBytes(entryModified);
                                        newStreamZip.Write(byteArrayEntryStream, 0, byteArrayEntryStream.Length);
                                    }
                            }

                        }
                    }
                    new ZipArchive(saveStream).ExtractToDirectory(settings.GetXamarinPath(settings.SlnTargetPath));
                }

                #endregion

                // Copy (overwriting) the directory WasNamespace.W10 -> WasNamespace.Xamarin\WasNamespace\WasNamespace\
                DirectoryCopy(settings.GetWin10Path(settings.SlnTargetPath),
                    Path.Combine(settings.GetXamarinPath(settings.SlnTargetPath), settings.NameSpace, settings.NameSpace),
                    true, true);

                // install base files (copy or update)
                var baseFilesToInstall = convertJson.BaseFiles;

                foreach (var file in baseFilesToInstall)
                {
                    file.Rename("WasAppNamespace", settings.NameSpace);
                    try
                    {
                        VerifyFilesToInclude(file);
                        file.Install();
                    }
                    catch (Exception e)
                    {
                        result += string.Format(ConvertResources.ErrorInstallFile, file.SourceFilename, file.Action == InstallType.Update ? ConvertResources.Modified : ConvertResources.Copied);
                    }
                }


                if (string.IsNullOrEmpty(result))
                    result = ConvertResources.SuccessBaseInstall;
            }
            if (settings.WithCollections)
            {
                var sectionFilesToInstall = convertJson.SectionFiles;

                string resultCollection = "";

                // for each collection (=section)...
                foreach (var collectionSectionName in settings.Collections)
                {
                    // ...install collection files (copy or update)
                    Match resultRegex = null;
                    bool alreadyUpdated = false;
                    try
                    {
                        alreadyUpdated = false; //Regex.IsMatch(fileTxt, ".*DetailViewModelWithCategories.*");
                        if (!alreadyUpdated)
                            resultRegex = Regex.Match(File.ReadAllText(
                                Path.Combine(settings.GetXamarinPath(settings.SlnTargetPath), settings.NameSpace, settings.NameSpace, ConvertResources.AppStudioSectionsDirectory, collectionSectionName + ConvertResources.AppStudioSectionEndingFilename + ".cs")),
                                $@"{collectionSectionName}Section *: *Section<([^>]*)Schema>");
                    }
                    catch (Exception)
                    {
                        resultRegex = null;
                    }
                    if (resultRegex == null || !resultRegex.Success || resultRegex.Groups.Count < 2)
                    {
                        resultCollection +=
                            string.Format(
                                alreadyUpdated
                                    ? ConvertResources.ErrorCollectionAlreadyUpdated
                                    : ConvertResources.ErrorFindingSection, collectionSectionName);
                    }
                    else
                    {
                        string collectionSchemaName = resultRegex.Groups[1].Value;
                        foreach (var originFile in sectionFilesToInstall)
                        {
                            if (originFile != null)
                            {
                                var file = originFile.Clone();

                                try
                                {
                                    file.Rename("WasAppNamespace", settings.NameSpace);
                                    file.Rename("WasAppSectionName", collectionSectionName);
                                    file.Rename("WasAppSchemaName", collectionSchemaName);

                                    VerifyFilesToInclude(file);

                                    file.Install();
                                }
                                catch (Exception)
                                {
                                    if (file.ShowError)
                                        resultCollection += string.Format(ConvertResources.ErrorInstallFile,
                                            file.SourceFilename, file.Action == InstallType.Update ? ConvertResources.Modified : ConvertResources.Copied);
                                }
                            }
                            else
                            {
                                resultCollection += ConvertResources.FatalErrorInstallFile;
                            }

                        }
                    }
                    result += string.IsNullOrEmpty(resultCollection)
                        ? string.Format(ConvertResources.SuccessCollectionInstall, collectionSectionName)
                        : resultCollection;
                }
            }
            return "\n" + result;
        }

        private static void VerifyFilesToInclude(FileToInstall file)
        {
            var searchReplace = file.SearchReplaceRegExDictionary;
            if (searchReplace != null && searchReplace.Count > 0)
            {
                searchReplace.Keys.ToList().ForEach(searchString =>
                {
                    // For CS Projects, verify if the files to include in the .csproj exists.
                    if (Path.GetExtension(file.TargetName) == ".csproj")
                    {
                        // search the XML tags that are not *Reference, with the unique attribut "Include" (ie Page, Content, EmbeddedResource, Compile or None)
                        // Note: the opened tag must be in a single line (ie: <Compile Include="/mydirectory/myfile.ext">)
                        var regexStringForFilesToCompile =
                            //"[ |\r|\n]*<Compile Include=\"([^\"]*)\" *(?:/>|>(?:(?:[ |\r|\n]*)(?!</Compile>).)*</Compile>)";
                            "\r\n[ \t]*<(?![a-zA-Z]+Reference)([a-zA-Z]+) *Include=\"([^\"]*)\" *(?:>(?:.*?)<\\/\\1>|\\/>)";
                        var allFilesToCompile =
                            new Regex(regexStringForFilesToCompile, RegexOptions.Singleline).Matches(searchReplace[searchString]);
                        foreach (Match fileToCompile in allFilesToCompile)
                            if (
                                !File.Exists(Path.Combine(Path.GetDirectoryName(file.TargetFilename),
                                    fileToCompile.Groups[2].Value)))
                                file.RenameInDictionary(fileToCompile.Groups[0].Value, "", false, true);
                    }
                });
            }
        }
    }
}
