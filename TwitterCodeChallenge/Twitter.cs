using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace TwitterCodeChallenge
{
  
    public class TwitterToken
    {
        public string Token { get; set; }
        public string Secret { get; set; }
        public string UserId { get; set; }
        public string ScreenName { get; set; }
    }

    public class Twitter
    {
        public static string Tweet(string status, string token, string tokenSecret)
        {
            return SendRequest("statuses/update.json?status=" + status, "POST", null, token, tokenSecret);
        }

        public static string GetTweets(string screenName, int count, string token, string tokenSecret)
        {
            return SendRequest($"statuses/user_timeline.json?screen_name={screenName}&count={count}", "GET", null, token, tokenSecret);
        }

        public static string GetTweetsByPlace(string place, int count, string token, string tokenSecret)
        {
            return SendRequest($"search/tweets.json?q=place%3A{place}&count={count}", "GET", null, token, tokenSecret);
        }

        public static string GetTweetsFromUserWithHashtag(string user, string hashtag, int count, string token, string tokenSecret)
        {
            return SendRequest($"search/tweets.json?q=from%3A{user}%20%23{hashtag}&count={count}", "GET", null, token, tokenSecret);
        }

        public static TwitterToken GetAccessToken(string oauthVerifier, string token, string tokenSecret)
        {
            var response = SendRequest("https://api.twitter.com/oauth/access_token", "POST", $"oauth_verifier={oauthVerifier}", token, tokenSecret);
            var queryString = HttpUtility.ParseQueryString(response);

            return new TwitterToken() {
                Token = queryString["oauth_token"],
                Secret = queryString["oauth_token_secret"],
                UserId = queryString["user_id"],
                ScreenName = queryString["screen_name"]
            };
        }

        public static TwitterToken GetOAuthToken(string callback)
        {
            //https://dev.twitter.com/web/sign-in/implementing
            var response = SendRequest("https://api.twitter.com/oauth/request_token", "POST", null, null, null, callback);
            var queryString = HttpUtility.ParseQueryString(response);
            return new TwitterToken() { Token = queryString["oauth_token"], Secret = queryString["oauth_token_secret"] };
            }


        private static byte[] HMACSHA1(string message, string secret)
        {
            var encoding = new ASCIIEncoding();
            var keyBytes = encoding.GetBytes(secret);
            var messageBytes = encoding.GetBytes(message);
            var hmac = new HMACSHA1(keyBytes);

            return hmac.ComputeHash(messageBytes);
        }


        private static string CalculateSignature(string url, string method, string body, string nonce, string timestamp, string consumerKey, string token, string consumerSecret, string tokenSecret, string callback = null)
        {
            var querystring = string.Empty;
            if(url.Contains("?"))
            {
                querystring = url.Substring(url.IndexOf("?") +1);
                url = url.Substring(0, url.IndexOf("?"));
            }
            

            var parameterDict = new Dictionary<string, string>
            {
                { "oauth_consumer_key", consumerKey },
                { "oauth_nonce", nonce },
                { "oauth_signature_method" , "HMAC-SHA1" },
                { "oauth_timestamp" , timestamp },
                { "oauth_token" , token },
                { "oauth_version" , "1.0" }
            };

            if (!string.IsNullOrEmpty(callback))
                parameterDict.Add("oauth_callback", callback);

            if(!string.IsNullOrEmpty(querystring))
            {
                foreach (var qsParam in querystring.Split('&'))
                {
                    parameterDict.Add(Uri.EscapeDataString(qsParam.Split('=')[0]), 
                        Uri.EscapeDataString(Uri.UnescapeDataString(qsParam.Split('=')[1])));
                }
            }

            if (!string.IsNullOrEmpty(body))
            {
                foreach (var param in body.Split('&'))
                {
                    parameterDict.Add(Uri.EscapeDataString(param.Split('=')[0]), Uri.EscapeDataString(param.Split('=')[1]));
                }
            }

            var index = 0;
            var parameterString = new StringBuilder();
            foreach(var param in parameterDict.OrderBy(i => i.Key))
            {
                if (index > 0)
                    parameterString.Append("&");

                parameterString.AppendFormat("{0}={1}",param.Key, param.Value);
                index++;
            }

            var str = $"{method.ToUpper()}&{Uri.EscapeDataString(url)}&{Uri.EscapeDataString(parameterString.ToString())}";
            var hmac = HMACSHA1(str, $"{consumerSecret}&{tokenSecret}");
            return Convert.ToBase64String(hmac);
        }


        private static string GetConfigValue(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        public static string SendRequest(string url, string method, string body = null, string token = null, string tokenSecret = null, string callback = null)
        {
            var requestUrl = url.StartsWith("http") ? url : "https://api.twitter.com/1.1/" + url;

            var httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUrl);

            //https://dev.twitter.com/oauth/overview/authorizing-requests
            var consumerKey = ConfigurationManager.AppSettings["TWITTER_CONSUMER_KEY"];
            var consumerSecret = ConfigurationManager.AppSettings["TWITTER_CONSUMER_SECRET"];
            var nonce = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
            var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            token = string.IsNullOrEmpty(token) ? ConfigurationManager.AppSettings["TWITTER_TOKEN"] : token;
            tokenSecret = string.IsNullOrEmpty(tokenSecret) ? ConfigurationManager.AppSettings["TWITTER_TOKEN_SECRET"] : tokenSecret;

            var signature = CalculateSignature(requestUrl, method, body, nonce, timestamp, consumerKey, token, consumerSecret, tokenSecret, callback);

            var authHeader = $"OAuth " +

              (!string.IsNullOrEmpty(callback) ? $"oauth_callback=\"{callback}\", " : "") +
              $"oauth_consumer_key=\"{Uri.EscapeDataString(consumerKey)}\", " +
              $"oauth_nonce=\"{nonce}\", " +
              $"oauth_signature=\"{Uri.EscapeDataString(signature)}\", " +
               "oauth_signature_method=\"HMAC-SHA1\", " +
              $"oauth_timestamp=\"{timestamp}\", " +
              $"oauth_token=\"{Uri.EscapeDataString(token)}\", " +
              "oauth_version=\"1.0\"";

            httpWebRequest.Headers["Authorization"] = authHeader;

            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            httpWebRequest.Method = method;

            var response = string.Empty;

            if (!string.IsNullOrEmpty(body))
            {
                using (var req = httpWebRequest.GetRequestStream())
                {
                    var bodyBytes = Encoding.UTF8.GetBytes(body);
                    req.Write(bodyBytes, 0, bodyBytes.Length);
                }
            }

            try
            {
                using (var res = httpWebRequest.GetResponse())
                {
                    using (var str = res.GetResponseStream())
                    {
                        using (var reader = new StreamReader(str))
                        {
                            return response = reader.ReadToEnd();
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                using (HttpWebResponse res = (HttpWebResponse)ex.Response)
                {
                    if (res == null)
                        throw new TwitterException("No response was returned from Twitter");

                    using (var str = res.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(str))
                        {
                            response = reader.ReadToEnd();
                            throw new TwitterException(response);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new TwitterException(ex.Message);
            }  
        }

        public class TwitterException : Exception
        {
            public TwitterException(string message) : base(message)
            {

            }
        }

}
}