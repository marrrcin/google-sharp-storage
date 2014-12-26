using System;
using Google.Apis.Storage.v1;

namespace GoogleSharpStorage
{
    public enum Access
    {
        Public,
        ProjectPrivate,
        Private,
        Authenticated,
        BucketOwner,
        BucketOwnerFull
    }

    public static class AccessMap
    {
        public static ObjectsResource.InsertMediaUpload.PredefinedAclEnum ForUpload(this Access access)
        {
            switch (access)
            {
                case Access.Public:
                    return ObjectsResource.InsertMediaUpload.PredefinedAclEnum.PublicRead;

                case Access.Authenticated:
                    return ObjectsResource.InsertMediaUpload.PredefinedAclEnum.AuthenticatedRead;

                case Access.Private:
                    return ObjectsResource.InsertMediaUpload.PredefinedAclEnum.Private;

                case Access.BucketOwner:
                    return ObjectsResource.InsertMediaUpload.PredefinedAclEnum.BucketOwnerRead;

                case Access.BucketOwnerFull:
                    return ObjectsResource.InsertMediaUpload.PredefinedAclEnum.BucketOwnerFullControl;

                case Access.ProjectPrivate:
                    return ObjectsResource.InsertMediaUpload.PredefinedAclEnum.ProjectPrivate;
                default:
                    throw new Exception("Unknown access level : " + access);

            }
        }
    }
}
