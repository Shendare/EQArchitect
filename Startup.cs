using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(EQArchitect.Startup))]
namespace EQArchitect
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
