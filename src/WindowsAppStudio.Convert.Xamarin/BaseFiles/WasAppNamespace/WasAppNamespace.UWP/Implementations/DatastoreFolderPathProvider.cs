using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Acquaint.Abstractions;

namespace WasAppNamespace.UWP
{
    public class DatastoreFolderPathProvider : IDatastoreFolderPathProvider
    {
        public string GetPath()
        {
            return Windows.Storage.ApplicationData.Current.LocalFolder.Path;
        }
    }
}
