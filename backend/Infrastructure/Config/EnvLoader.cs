using DotNetEnv;
namespace backend.Infrastructure.Config
{
    public static class EnvLoader
    {
        public static void Load()
        {
            Env.Load();
        }
    }
}
