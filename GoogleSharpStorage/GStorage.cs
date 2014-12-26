using System.Collections.Specialized;
using System.Web;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Web;
using Google.Apis.Storage.v1;
using Google.Apis.Storage.v1.Data;
using Google.Apis.Upload;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Object = Google.Apis.Storage.v1.Data.Object;
namespace GoogleSharpStorage
{
    public class GStorage
    {
        protected ClientSecrets ClientSecrets;

        protected string Mail;

        protected string[] DefaultScopes = new[] { @"https://www.googleapis.com/auth/devstorage.full_control" };

        protected GoogleAuthorizationCodeFlow AuthorizationFlow;
        public ServiceAccountCredential ServiceAccount { protected set; get; }

        public UserCredential UserCredential { protected set; get; }

        public StorageService Service { protected set; get; }

        public string ProjectName { set; get; }

        protected string AccessToken
        {
            get
            {
                if (UserCredential != null)
                {
                    return UserCredential.Token.AccessToken;
                }
                else if (ServiceAccount != null)
                {
                    return ServiceAccount.Token.AccessToken;
                }
                else
                {
                    throw new UnauthorizedAccessException("There is no User or Service account token!");
                }
            }
        }

        public IEnumerable<string> Scopes { set; get; }

        /// <summary>
        /// Creates GStorage - a Google Cloud Storage Helper for use with classical authentication which requires user interaction
        /// </summary>
        /// <param name="id">OAuth2 Client ID</param>
        /// <param name="secret">OAuth2 Client Secret - if you use SERVICE ACCOUNT you can skip this param</param>
        /// <param name="email">Gmail address connected do Google Cloud Storage</param>
        /// <param name="projName">Google Developers Console Project Name</param>
        public GStorage(string id,string email,string secret = null,string projName = null)
        {
            ClientSecrets = new ClientSecrets {ClientId = id, ClientSecret = secret};
            Mail = email;

            ProjectName = projName;

            Scopes = DefaultScopes;

            Service = new StorageService();
        }

        public GStorage(GStorageParams initialParams) : this(initialParams.ClientId,initialParams.Mail,initialParams.ClientSecret,initialParams.Project)
        {
            Scopes = initialParams.Scopes ?? DefaultScopes;
        }

        /// <summary>
        /// Returns IEnumerable of Buckets from given project.
        /// This method is async and can be used with await.
        /// </summary>
        /// <param name="projectName">name of the project</param>
        /// <returns></returns>
        public async Task<IEnumerable<Bucket>> BucketsAsync(string projectName = null)
        {
            var bucketsQuery = PrepareBucketsQuery(projectName);
            var buckets = await bucketsQuery.ExecuteAsync();
            return buckets.Items;
        }

        /// <summary>
        /// Returns IEnumerable of Buckets from given project.
        /// This method blocks the thread until finished.
        /// </summary>
        /// <param name="projectName">name of the project</param>
        /// <returns></returns>
        public IEnumerable<Bucket> Buckets(string projectName = null)
        {
            return BucketsAsync(projectName).Result;
        }

        protected virtual BucketsResource.ListRequest PrepareBucketsQuery(string projectName)
        {
            var bucketsQuery = Service.Buckets.List(projectName ?? ProjectName);
            bucketsQuery.OauthToken = AccessToken;
            return bucketsQuery;
        }

        public async void CreateBucket(string bucketName,string projectName = null)
        {
            var newBucket = new Bucket()
            {
                Name = bucketName
            };

            var newBucketQuery = Service.Buckets.Insert(newBucket, projectName ?? ProjectName);
            newBucketQuery.OauthToken = AccessToken;
            await newBucketQuery.ExecuteAsync();
        }

        /// <summary>
        /// Returns IEnumerable of objects in given bucket. Objects are items from the bucket wrapped in C# objects with all of the properties.
        /// This method is async and can be used with await.
        /// </summary>
        /// <param name="bucketName">Name of the bucket</param>
        /// <returns></returns>
        public async Task<IEnumerable<Object>> ObjectsAsync(string bucketName)
        {
            var objectsQuery = PrepareObjectsQuery(bucketName);
            var objects =  await objectsQuery.ExecuteAsync();
            return objects.Items;
        }

        /// <summary>
        /// Returns IEnumerable of objects in given bucket. Objects are items from the bucket wrapped in C# objects with all of the properties.
        /// This method blocks the thread until finished.
        /// </summary>
        /// <param name="bucketName">Name of the bucket</param>
        /// <returns></returns>
        public IEnumerable<Object> Objects(string bucketName)
        {
            var objectsQuery = PrepareObjectsQuery(bucketName);
            var objects = objectsQuery.Execute();
            return objects.Items;
        }
        protected virtual ObjectsResource.ListRequest PrepareObjectsQuery(string bucketName)
        {
            var objectsQuery = Service.Objects.List(bucketName);
            objectsQuery.OauthToken = AccessToken;
            return objectsQuery;
        }

        /// <summary>
        /// Updates meta data of an object.
        /// </summary>
        /// <param name="obj">object to update, with new meta data</param>
        /// <returns></returns>
        public Object UpdateObject(Object obj)
        {
            var updateQuery = Service.Objects.Update(obj, obj.Bucket, obj.Name);
            updateQuery.OauthToken = AccessToken;
            var updated = updateQuery.Execute();
            return updated;
        }


        #region UploadFileSync

        /// <summary>
        /// Uploads file from given stream to Google Cloud Storage with specified file name.
        /// This method block the thread until finished.
        /// </summary>
        /// <param name="bucketName">bucket to which the file will be uploaded</param>
        /// <param name="fileName">name of the file after upload</param>
        /// <param name="fileStream">stream of the file</param>
        public void UploadFile(string bucketName, string fileName,Stream fileStream)
        {
            PrepareUploadRequest(bucketName, fileName, fileStream).Upload();
        }

        /// <summary>
        /// Uploads file from given stream to Google Cloud Storage with specified file name.
        /// This method block the thread until finished.
        /// </summary>
        /// <param name="bucketName">bucket to which the file will be uploaded</param>
        /// <param name="fileName">name of the file after upload</param>
        /// <param name="fileStream">stream of the file</param>
        /// <param name="accessRights">access rights to file</param>
        public void UploadFile(string bucketName, string fileName, Stream fileStream, Access accessRights)
        {
            PrepareUploadRequest(bucketName, fileName, fileStream,access:accessRights).Upload();
        }

        /// <summary>
        /// Uploads file from given stream to Google Cloud Storage with specified file name.
        /// This method block the thread until finished.
        /// </summary>
        /// <param name="bucketName">bucket to which the file will be uploaded</param>
        /// <param name="fileName">name of the file after upload</param>
        /// <param name="fileStream">stream of the file</param>
        /// <param name="onProgressChanged">action invoked when upload progress changes</param>
        public void UploadFile(string bucketName, string fileName, Stream fileStream,Action<IUploadProgress> onProgressChanged)
        {
            PrepareUploadRequest(bucketName, fileName, fileStream, onProgressChanged).Upload();
        }

        /// <summary>
        /// Uploads file from given stream to Google Cloud Storage with specified file name.
        /// This method block the thread until finished.
        /// </summary>
        /// <param name="bucketName">bucket to which the file will be uploaded</param>
        /// <param name="fileName">name of the file after upload</param>
        /// <param name="fileStream">stream of the file</param>
        /// <param name="success">action invoked after recieving response - you have access to uploaded Object and it's parameters like URL</param>
        public void UploadFile(string bucketName, string fileName, Stream fileStream, Action<Object> success)
        {
            PrepareUploadRequest(bucketName, fileName, fileStream,null,success).Upload();
        }

        /// <summary>
        /// Uploads file from given stream to Google Cloud Storage with specified file name.
        /// This method block the thread until finished.
        /// </summary>
        /// <param name="bucketName">bucket to which the file will be uploaded</param>
        /// <param name="fileName">name of the file after upload</param>
        /// <param name="fileStream">stream of the file</param>
        /// <param name="onProgressChanged">action invoked when upload progress changes</param>
        /// <param name="accessRights">access rights to file</param>
        public void UploadFile(string bucketName, string fileName, Stream fileStream,
            Action<IUploadProgress> onProgressChanged,Access accessRights)
        {
            PrepareUploadRequest(bucketName, fileName, fileStream, onProgressChanged,access:accessRights).Upload();
        }

        /// <summary>
        /// Uploads file from given stream to Google Cloud Storage with specified file name.
        /// This method block the thread until finished.
        /// </summary>
        /// <param name="bucketName">bucket to which the file will be uploaded</param>
        /// <param name="fileName">name of the file after upload</param>
        /// <param name="fileStream">stream of the file</param>
        /// <param name="access">access type (default is public)</param>
        /// <param name="success">action invoked after recieving response - you have access to uploaded Object and it's parameters like URL</param>
        /// <param name="onProgressChanged">action which will be invoked when OnProgresChanged event of the upload process will fire</param>
        public void UploadFile(string bucketName, string fileName, Stream fileStream,
             Action<Object> success, Action<IUploadProgress> onProgressChanged, Access access)
        {
            var uploadRequest = PrepareUploadRequest(bucketName, fileName, fileStream, onProgressChanged, success, access);
            uploadRequest.Upload();
        }

        /// <summary>
        /// Uploads file from given stream to Google Cloud Storage with specified file name and mime-type.
        /// This method block the thread until finished.
        /// </summary>
        /// <param name="bucketName">bucket to which the file will be uploaded</param>
        /// <param name="fileName">name of the file after upload</param>
        /// <param name="fileStream">stream of the file</param>
        /// <param name="access">access type (default is public)</param>
        /// <param name="success">action invoked after recieving response - you have access to uploaded Object and it's parameters like URL</param>
        /// <param name="onProgressChanged">action which will be invoked when OnProgresChanged event of the upload process will fire</param>
        /// <param name="mimeType">mime type of the file</param>
        public void UploadFile(string bucketName, string fileName, Stream fileStream,
             Action<Object> success, Action<IUploadProgress> onProgressChanged, Access access,string mimeType)
        {
            var uploadRequest = PrepareUploadRequest(bucketName, fileName, fileStream, onProgressChanged, success, access,mimeType);
            uploadRequest.Upload();
        }

        #endregion

        #region UploadFileAsync

        /// <summary>
        /// Uploads file from given stream to Google Cloud Storage with specified file name.
        /// This method is async and in can be used with await.
        /// </summary>
        /// <param name="bucketName">bucket to which the file will be uploaded</param>
        /// <param name="fileName">name of the file after upload</param>
        /// <param name="fileStream">stream of the file</param>
        public async Task UploadFileAsync(string bucketName, string fileName, Stream fileStream)
        {
            await PrepareUploadRequest(bucketName, fileName, fileStream).UploadAsync();
        }

        /// <summary>
        /// Uploads file from given stream to Google Cloud Storage with specified file name.
        /// This method is async and in can be used with await.
        /// </summary>
        /// <param name="bucketName">bucket to which the file will be uploaded</param>
        /// <param name="fileName">name of the file after upload</param>
        /// <param name="fileStream">stream of the file</param>
        /// <param name="accessRights">access rights to file</param>
        public async Task UploadFileAsync(string bucketName, string fileName, Stream fileStream, Access accessRights)
        {
            await PrepareUploadRequest(bucketName, fileName, fileStream, access: accessRights).UploadAsync();
        }

        /// <summary>
        /// Uploads file from given stream to Google Cloud Storage with specified file name.
        /// This method is async and in can be used with await.
        /// </summary>
        /// <param name="bucketName">bucket to which the file will be uploaded</param>
        /// <param name="fileName">name of the file after upload</param>
        /// <param name="fileStream">stream of the file</param>
        /// <param name="onProgressChanged">action which will be invoked when OnProgresChanged event of the upload process will fire</param>
        public async Task UploadFileAsync(string bucketName, string fileName, Stream fileStream, Action<IUploadProgress> onProgressChanged)
        {
            await PrepareUploadRequest(bucketName, fileName, fileStream, onProgressChanged).UploadAsync();
        }

        /// <summary>
        /// Uploads file from given stream to Google Cloud Storage with specified file name.
        /// This method is async and in can be used with await.
        /// </summary>
        /// <param name="bucketName">bucket to which the file will be uploaded</param>
        /// <param name="fileName">name of the file after upload</param>
        /// <param name="fileStream">stream of the file</param>
        /// <param name="success">action invoked after recieving response - you have access to uploaded Object and it's parameters like URL</param>
        public async Task UploadFileAsync(string bucketName, string fileName, Stream fileStream, Action<Object> success)
        {
            await PrepareUploadRequest(bucketName, fileName, fileStream, null,success).UploadAsync();
        }

        /// <summary>
        /// Uploads file from given stream to Google Cloud Storage with specified file name.
        /// This method is async and in can be used with await.
        /// </summary>
        /// <param name="bucketName">bucket to which the file will be uploaded</param>
        /// <param name="fileName">name of the file after upload</param>
        /// <param name="fileStream">stream of the file</param>
        /// <param name="onProgressChanged">action which will be invoked when OnProgresChanged event of the upload process will fire</param>
        /// <param name="accessRights">access rights to file</param>
        public async Task UploadFileAsync(string bucketName, string fileName, Stream fileStream,
           Action<IUploadProgress> onProgressChanged, Access accessRights)
        {
            await
                PrepareUploadRequest(bucketName, fileName, fileStream, onProgressChanged, access: accessRights)
                    .UploadAsync();
        }


        /// <summary>
        /// Uploads file from given stream to Google Cloud Storage with specified file name.
        /// This method is async and in can be used with await.
        /// </summary>
        /// <param name="bucketName">bucket to which the file will be uploaded</param>
        /// <param name="fileName">name of the file after upload</param>
        /// <param name="fileStream">stream of the file</param>
        /// <param name="access">access type (default is public)</param>
        /// <param name="success">action invoked after Recieving Response - you have access to uploaded Object and it's parameters like URL</param>
        /// <param name="onProgresChanged">action which will be invoked when OnProgresChanged event of the upload process will fire</param>
        public async Task UploadFileAsync(string bucketName, string fileName, Stream fileStream, Action<Object> success, Action<IUploadProgress> onProgresChanged, Access access)
        {
            var uploadRequest = PrepareUploadRequest(bucketName, fileName, fileStream, onProgresChanged,success,access);
            await uploadRequest.UploadAsync();
        }

        /// <summary>
        /// Uploads file from given stream to Google Cloud Storage with specified file name and mime-type.
        /// This method is async and in can be used with await.
        /// </summary>
        /// <param name="bucketName">bucket to which the file will be uploaded</param>
        /// <param name="fileName">name of the file after upload</param>
        /// <param name="fileStream">stream of the file</param>
        /// <param name="access">access type (default is public)</param>
        /// <param name="success">action invoked after Recieving Response - you have access to uploaded Object and it's parameters like URL</param>
        /// <param name="onProgresChanged">action which will be invoked when OnProgresChanged event of the upload process will fire</param>
        /// <param name="mimeType">mime type of the file</param>
        public async Task UploadFileAsync(string bucketName, string fileName, Stream fileStream, Action<Object> success, Action<IUploadProgress> onProgresChanged, Access access,string mimeType)
        {
            var uploadRequest = PrepareUploadRequest(bucketName, fileName, fileStream, onProgresChanged, success, access,mimeType);
            await uploadRequest.UploadAsync();
        }

        #endregion

        protected virtual ObjectsResource.InsertMediaUpload PrepareUploadRequest(string bucketName, string fileName, Stream fileStream, Action<IUploadProgress> onProgresChanged = null, Action<Object> success = null, Access access = Access.Public,string mimeType = null)
        {
            var newObject = new Object()
            {
                Bucket = bucketName,
                Name = fileName,

            };

            mimeType = mimeType ?? MimeMapping.GetMimeMapping(fileName);

            var uploadRequest = new ObjectsResource.InsertMediaUpload(Service, newObject, bucketName, fileStream,
                mimeType);

            uploadRequest.OauthToken = AccessToken;
            uploadRequest.PredefinedAcl = access.ForUpload();

            if (onProgresChanged != null)
            {
                uploadRequest.ProgressChanged += onProgresChanged;
            }

            if (success != null)
            {
                uploadRequest.ResponseReceived += success;
            }

            return uploadRequest;
        }


        /// <summary>
        /// Downloads the file from the bucket.
        /// Returns a stream for further processing.
        /// </summary>
        /// <param name="bucketName">name of the bucket</param>
        /// <param name="fileName">name of the file in the bucket</param>
        /// <returns></returns>
        public async Task<Stream> DownloadFile(string bucketName, string fileName)
        {
            var downloadRequest = new ObjectsResource.GetRequest(Service, bucketName, fileName);
            downloadRequest.OauthToken = AccessToken;

            var file = await downloadRequest.ExecuteAsync();
            var stream = new MemoryStream();
            await downloadRequest.MediaDownloader.DownloadAsync(file.MediaLink, stream);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        /// <summary>
        /// Gets Service Authorization for application. Service authorization allows
        /// API calls without user interaction.
        /// </summary>
        /// <param name="certificatePath">path to certificate (.p12 file)</param>
        public virtual void ServiceAuthorize(string certificatePath)
        {
            var certificate = new X509Certificate2(certificatePath,"notasecret",X509KeyStorageFlags.MachineKeySet|X509KeyStorageFlags.Exportable);
            ServiceAuthorize(certificate);
        }

        /// <summary>
        /// Gets Service Authorization for application. Service authorization allows
        /// API calls without user interaction.
        /// </summary>
        /// <param name="certificate">certificate (loaded from .p12 file) which you can get on Google developers console</param>
        public virtual void ServiceAuthorize(X509Certificate2 certificate)
        {
            var sac = new ServiceAccountCredential(new ServiceAccountCredential.Initializer(Mail)
            {
                Scopes = Scopes
            }.FromCertificate(certificate));
            var cts = new CancellationToken();
            var response = sac.RequestAccessTokenAsync(cts).Result;
            if (!response)
            {
                throw new UnauthorizedAccessException("Could not authorize service account!");
            }
            ServiceAccount = sac;
        }

        /// <summary>
        /// Authorizes user using OAuth2.0. This method is dedicated for Console/WPF apps.
        /// This method is async and can be used with await.
        /// </summary>
        public virtual void Authorize()
        {
            AuthorizeAsync().Wait();
        }

        /// <summary>
        /// Authorizes user using OAuth2.0. This method is dedicated for Console/WPF apps.
        /// This method blocks the thread until finished.
        /// </summary>
        public async virtual Task AuthorizeAsync()
        {
            var cts = new CancellationTokenSource();
            UserCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(ClientSecrets, Scopes, Mail, cts.Token);
        }

        /// <summary>
        /// Starts the OAuth2.0 authorization process for web applications.
        /// Returns url to which client should be  redirected in order to give your app the access to Google Account.
        /// </summary>
        /// <param name="redirectUrl">postback url - google authorization will redirect back to this url</param>
        public virtual string AuthorizeWebAppBegin(string redirectUrl)
        {
            AuthorizationFlow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = ClientSecrets,
                Scopes = Scopes
            });
            var request = AuthorizationFlow.CreateAuthorizationCodeRequest(redirectUrl);
            var uri = request.Build();
            return uri.ToString();
        }

        /// <summary>
        /// Ends the OAuth2.0 authorization process for web applications.
        /// After successful execution of this method you are able to
        /// perform actions like upload/download on GStorage object
        /// </summary>
        /// <param name="request">current HttpRequest from web application</param>
        /// <param name="redirectUrl">url to which redirection will be made after successful authorization</param>
        public virtual void AuthorizeWebAppEnd(HttpRequestBase request,string redirectUrl)
        {
            var code = GetCodeFromRequest(request);
            AuthorizeWebAppEnd(code, redirectUrl);
        }

        /// <summary>
        /// Ends the OAuth2.0 authorization process for web applications.
        /// After successful execution of this method you are able to
        /// perform actions like upload/download on GStorage object
        /// </summary>
        /// <param name="code">code parameter extracted from HttpRequest query string</param>
        /// <param name="redirectUrl">url to which redirection will be made after successful authorization</param>
        public virtual void AuthorizeWebAppEnd(string code,string redirectUrl)
        {
            if (AuthorizationFlow == null)
            {
                throw new NullReferenceException("Authorization not started!");
            }

            var tokenRequest = AuthorizationFlow.ExchangeCodeForTokenAsync(Mail, code, redirectUrl, CancellationToken.None);
            tokenRequest.Wait();
            UserCredential = new UserCredential(AuthorizationFlow,Mail,tokenRequest.Result);
        }

        protected virtual string GetCodeFromRequest(HttpRequestBase request)
        {
            var code = request.Params["code"];
            if (string.IsNullOrEmpty(code))
            {
                throw new NullReferenceException("Access code not found in request!");
            }
            return code;
        }
        
        public virtual void RefreshAuthorization()
        {
            RefreshAuthorizationAsync().Wait();
        }

        public async virtual Task RefreshAuthorizationAsync()
        {
            var cts = new CancellationTokenSource();
            await UserCredential.RefreshTokenAsync(cts.Token);
        }
    }

    public class GStorageParams
    {
        public string ClientId { set; get; }
        public string ClientSecret { set; get; }
        public string ServiceId { set; get; }
        public string Mail { set; get; }
        public string Project { set; get; }

        public string[] Scopes { set; get; }
    }
}
