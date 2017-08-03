using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(TwitterCodeChallenge.Startup))]
namespace TwitterCodeChallenge
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
