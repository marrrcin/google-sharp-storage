using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.UI;
using GoogleSharpStorage;

namespace SampleWebApp.Controllers
{
    public class HomeController : Controller
    {
        private string _httpLocalhost = @"http://localhost:19576/";

        public ActionResult Index()
        {
            var isStorageInSession = Session["storage"] != null;
            ViewBag.AuthDone = false;
            if (isStorageInSession)
            {
                var storage = Session["storage"] as GStorage;
                if (storage != null)
                {
                    storage.AuthorizeWebAppEnd(Request, _httpLocalhost);
                    ViewBag.AuthDone = true;
                }
            }

            return View();
        }

        [HttpPost]
        public ActionResult UploadFileOAuth(HttpPostedFileBase file)
        {
            var storage = Session["storage"] as GStorage;
            if (storage != null)
            {
                string fileName = "test.jpg";
                string bucket = "xxxxx";
                string url = "";

                storage.UploadFile(bucket, fileName, file.InputStream, success => { url = success.PublicLink(); });

                return RedirectToAction("Success", "Home", new { url = url });
            }
            return null;
        }

        public ActionResult Success(string url)
        {
            ViewBag.Url = url;
            return View("Success");
        }

        [HttpPost]
        public ActionResult UploadFileServiceAuthorization(HttpPostedFileBase file)
        {
            string clientIdService = "xxxxx.apps.googleusercontent.com";
            string serviceMail = @"xxxxx@developer.gserviceaccount.com";
            string project = "xxxxx-748";
            string authFile = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, "auth.p12");

            var storage = new GStorage(new GStorageParams
            {
                ClientId = clientIdService,
                Mail = serviceMail,
                Project = project
            });

            storage.ServiceAuthorize(authFile);

            string fileName = "test.jpg";
            string bucket = "xxxxx";
            string url = "";

            storage.UploadFile(bucket, fileName, file.InputStream, success => { url = success.PublicLink(); });

            return RedirectToAction("Success", "Home", new { url = url });
        }

        public ActionResult AuthorizeOAuth()
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

            var r = storage.AuthorizeWebAppBegin(_httpLocalhost);
            Session["storage"] = storage;
            return Redirect(r);
        }
    }
}