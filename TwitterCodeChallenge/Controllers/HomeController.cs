using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace TwitterCodeChallenge.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        //This is the User Home Page
        public ActionResult Index()
        { 
            var user = (ClaimsIdentity)User.Identity;
            if (!user.HasClaim("Role", "User"))
                return RedirectToAction("UserManagement", "Account");

            var twitterLoginInfo = Session["TwitterLogin"] as TwitterToken;
            
            if(twitterLoginInfo == null)
            {
                if(user.HasClaim(m => m.Type == "TwitterUserScreenName"))
                {
                    twitterLoginInfo = new TwitterToken()
                    {
                        ScreenName = user.Claims.FirstOrDefault(m => m.Type == "TwitterUserScreenName")?.Value,
                        Token = user.Claims.FirstOrDefault(m => m.Type == "TwitterToken")?.Value,
                        Secret = user.Claims.FirstOrDefault(m => m.Type == "TwitterTokenSecret")?.Value,
                        UserId = user.Claims.FirstOrDefault(m => m.Type == "TwitterUserId")?.Value,

                    };
                }

            }

            ViewBag.TwitterAvailable = twitterLoginInfo != null;
            ViewBag.TwitterScreenName = twitterLoginInfo != null ? twitterLoginInfo.ScreenName : "";
            ViewBag.TwitterAuthorization = twitterLoginInfo != null ? (twitterLoginInfo.Token + ":" + twitterLoginInfo.Secret) : "";
            ViewBag.Username = User.Identity.Name;
            return View();
        }
        
    }
}