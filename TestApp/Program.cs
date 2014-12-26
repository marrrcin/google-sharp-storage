using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using GoogleSharpStorage;
using Object = Google.Apis.Storage.v1.Data.Object;
namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var storage = WithServiceAccountAuthorization();
            //var storage = WithOAuthAuthorization();
            
            var url = UploadFile(storage);
            Console.WriteLine(url);
            Console.WriteLine("done");
            Console.ReadKey();
        }

        private static string UploadFile(GStorage storage)
        {
            string fileName = "test.jpg";
            string bucket = "xxxxx";
            string url = "";
            using (var file = new FileStream(fileName, FileMode.Open))
            {
                file.Position = 0;
                storage.UploadFile(bucket, fileName, file, success => { url = success.PublicLink(); },
                    null, Access.Public);
            }
            return url;
        }

        private static GStorage WithOAuthAuthorization()
        {
            string clientId = "xxxxx.apps.googleusercontent.com";
            string clientSecret = "xxxxx";
            string mail = @"xxxxx@gmail.com";
            string project = "xxxxx-748";

            var storage = new GStorage(new GStorageParams
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                Mail = mail,
                Project = project
            });

            storage.Authorize();
            storage.RefreshAuthorization();
            return storage;
        }

        private static GStorage WithServiceAccountAuthorization()
        {
            string clientIdService = "xxxxx.apps.googleusercontent.com";
            string serviceMail = @"xxxxx@developer.gserviceaccount.com";
            string project = "xxxxx-748";
            string authFile = "auth.p12";
            var storage = new GStorage(new GStorageParams
            {
                ClientId = clientIdService,
                Mail = serviceMail,
                Project = project
            });

            storage.ServiceAuthorize(authFile);
            return storage;
        }
    }
}
