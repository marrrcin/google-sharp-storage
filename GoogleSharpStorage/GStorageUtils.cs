using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Object = Google.Apis.Storage.v1.Data.Object;
namespace GoogleSharpStorage
{
    public static class GStorageUtils
    {
        private static readonly string BaseLink = @"http://storage.googleapis.com/";
        public static string PublicLink(this Object storageObject)
        {
            return BaseLink + storageObject.Bucket + "/" + storageObject.Name;
        }
    }
}
