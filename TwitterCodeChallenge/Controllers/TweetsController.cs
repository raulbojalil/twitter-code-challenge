using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using static TwitterCodeChallenge.Twitter;

namespace TwitterCodeChallenge.Controllers
{
    public class TweetsController : ApiController
    {
        private HttpResponseMessage GetError(Exception ex)
        {
            var errorCode = "500";
            HttpStatusCode httpCode = HttpStatusCode.InternalServerError;
            var errorMessage = "Internal Server Error";

            if (ex is UnauthorizedAccessException)
            {
                errorCode = "401";
                httpCode = HttpStatusCode.Unauthorized;
                errorMessage = ex.Message;
            }
            else if(ex is TwitterException)
            {
                errorCode = "1000";
                httpCode = HttpStatusCode.BadRequest;
                errorMessage = ex.Message;
            }

            var response = Request.CreateResponse(httpCode);
            response.Content = new StringContent("{ \"code\" : \"" + errorCode + "\", \"error\": \"" + errorMessage + "\" }", Encoding.UTF8, "application/json");
            return response;
        }

        private TwitterToken GetTwitterInfo()
        {
            
            var auth = Request.Headers.Where(m => m.Key == "X-TWITTERAUTH").FirstOrDefault();
            
            if (auth.Value == null)
                throw new UnauthorizedAccessException("Unauthorized");

            var authHeader = auth.Value.ElementAt(0);

            return new TwitterToken() { Token = authHeader.Split(':')[0], Secret = authHeader.Split(':')[1] };
        }

        private HttpResponseMessage GetJson(string json)
        {
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpGet]
        public HttpResponseMessage GetTweets()
        {
            try
            {
                var twitterInfo = GetTwitterInfo();
                return GetJson(Twitter.GetTweets(string.Empty, 10, twitterInfo.Token, twitterInfo.Secret));
            }
            catch(Exception ex)
            {
                return GetError(ex);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetTweetsByPlace(string place)
        {
            try
            {
                var twitterInfo = GetTwitterInfo();
                return GetJson(Twitter.GetTweetsByPlace(place, 5, twitterInfo.Token, twitterInfo.Secret));
            }
            catch (Exception ex)
            {
                return GetError(ex);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetTweetsFromUserWithHashtag(string user, string hashtag)
        {
            try
            {
                var twitterInfo = GetTwitterInfo();
                return GetJson(Twitter.GetTweetsFromUserWithHashtag(user, hashtag, 5, twitterInfo.Token, twitterInfo.Secret));
            }
            catch (Exception ex)
            {
                return GetError(ex);
            }

        }

        [HttpPost]
        public dynamic AddTweet(string status)
        {
            try
            {
                var twitterInfo = GetTwitterInfo();
                return GetJson(Twitter.Tweet(status, twitterInfo.Token, twitterInfo.Secret));
            }
            catch (Exception ex)
            {
                return GetError(ex);
            }

        }
    }
}