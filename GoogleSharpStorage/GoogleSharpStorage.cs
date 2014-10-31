using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Storage.v1;
using Google.Apis.Storage.v1.Data;
using Google.Apis.Upload;
using Object = Google.Apis.Storage.v1.Data.Object;

namespace GoogleStorageWrapper
{
    public class GoogleSharpStorage
    {
        private ClientSecrets clientSecrets;

        private string mail;

        public UserCredential UserCredential { private set; get; }

        public StorageService Service { private set; get; }
        public string ProjectName { set; get; }

        public IEnumerable<string> Scopes { set; get; }

        /// <summary>
        /// Creates GoogleSharpStorage - a Google Cloud Storage Helper
        /// </summary>
        /// <param name="id">OAuth2 Client ID</param>
        /// <param name="secret">OAuth2 Client Secret</param>
        /// <param name="email">Gmail address connected do Google Cloud Storage</param>
        /// <param name="projName">Google Developers Console Project Name</param>
        public GoogleSharpStorage(string id,string secret,string email,string projName = null)
        {
            clientSecrets = new ClientSecrets();
            clientSecrets.ClientId = id;
            clientSecrets.ClientSecret = secret;
            mail = email;

            Scopes = new[] { @"https://www.googleapis.com/auth/devstorage.full_control" };

            Service = new StorageService();
        }


        public async Task<IEnumerable<Bucket>> Buckets(string projectName = null)
        {
            var bucketsQuery = Service.Buckets.List(projectName ?? ProjectName);
            bucketsQuery.OauthToken = UserCredential.Token.AccessToken;
            var buckets = await bucketsQuery.ExecuteAsync();
            return buckets.Items;
        }

        public async void CreateBucket(string bucketName,string projectName = null)
        {
            var newBucket = new Bucket()
            {
                Name = bucketName
            };

            var newBucketQuery = Service.Buckets.Insert(newBucket, projectName ?? ProjectName);
            newBucketQuery.OauthToken = UserCredential.Token.AccessToken;
            await newBucketQuery.ExecuteAsync();
        }

        public async Task<IEnumerable<Object>> Objects(string bucketName, string projectName = null)
        {
            var objectsQuery = Service.Objects.List(bucketName);
            objectsQuery.OauthToken = UserCredential.Token.AccessToken;
            var objects =  await objectsQuery.ExecuteAsync();
            return objects.Items;
        }

        /// <summary>
        /// Uploads file from given stream to Google Cloud Storage with specified file name and mime-type
        /// </summary>
        /// <param name="bucketName">bucket to which the file will be uploaded</param>
        /// <param name="fileName">name of the file after upload</param>
        /// <param name="fileStream">stream of the file</param>
        /// <param name="mimeType">mime type of the file</param>
        /// <param name="onProgresChanged">action which will be invoked when OnProgresChanged event of the upload process will fire</param>
        public async void UploadFile(string bucketName, string fileName, Stream fileStream, string mimeType,Action<IUploadProgress> onProgresChanged = null)
        {
            var newObject = new Object()
            {
                Bucket = bucketName,
                Name = fileName
            };

            var uploadRequest = new ObjectsResource.InsertMediaUpload(Service, newObject, bucketName, fileStream,
                mimeType);
            uploadRequest.OauthToken = UserCredential.Token.AccessToken;

            if (onProgresChanged != null)
            {
                uploadRequest.ProgressChanged += onProgresChanged;
            }

            await uploadRequest.UploadAsync();
        }

        public async Task<Stream> DownloadFile(string bucketName, string fileName)
        {
            var downloadRequest = new ObjectsResource.GetRequest(Service, bucketName, fileName);
            downloadRequest.OauthToken = UserCredential.Token.AccessToken;

            var file = await downloadRequest.ExecuteAsync();
            var stream = new MemoryStream();
            await downloadRequest.MediaDownloader.DownloadAsync(file.MediaLink, stream);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public async virtual Task Authorize()
        {
            var cts = new CancellationTokenSource();
            UserCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(clientSecrets, Scopes, mail, cts.Token);
        }

        public async virtual Task RefreshAuthorization()
        {
            var cts = new CancellationTokenSource();
            await UserCredential.RefreshTokenAsync(cts.Token);
        }
    }
}
