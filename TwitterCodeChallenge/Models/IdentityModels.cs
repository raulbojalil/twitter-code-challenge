using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace TwitterCodeChallenge.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public string Role { get; set; } //Admin or User
        public string TwitterToken { get; set; }
        public string TwitterTokenSecret { get; set; }
        public string TwitterUserScreenName { get; set; }
        public string TwitterUserId { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            userIdentity.AddClaim(new Claim("Role", Role));
            if(!string.IsNullOrEmpty(TwitterToken)) userIdentity.AddClaim(new Claim("TwitterToken", TwitterToken));
            if(!string.IsNullOrEmpty(TwitterTokenSecret)) userIdentity.AddClaim(new Claim("TwitterTokenSecret", TwitterTokenSecret));
            if(!string.IsNullOrEmpty(TwitterUserScreenName)) userIdentity.AddClaim(new Claim("TwitterUserScreenName", TwitterUserScreenName));
            if(!string.IsNullOrEmpty(TwitterUserId)) userIdentity.AddClaim(new Claim("TwitterUserId", TwitterUserId));
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }
}