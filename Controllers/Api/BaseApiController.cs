using System.Web.Http;
using FileSharePortal.Models;

namespace FileSharePortal.Controllers.Api
{
    public class BaseApiController : ApiController
    {
        protected User CurrentUser
        {
            get
            {
                if (Request.Properties.ContainsKey("User"))
                {
                    return Request.Properties["User"] as User;
                }
                return null;
            }
        }

        protected int? CurrentApplicationId
        {
            get
            {
                if (Request.Properties.ContainsKey("ApplicationId"))
                {
                    return (int)Request.Properties["ApplicationId"];
                }
                return null;
            }
        }
    }
}
