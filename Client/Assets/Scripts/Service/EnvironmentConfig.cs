namespace PuzzleParty.Service
{
    public enum AppEnvironment
    {
        Dev,    // Unity Editor - talks to a local server (`dotnet run`), which itself points at
                // the Stage database via appsettings.Development.json. No setup needed here.
        Stage,  // Device build with the PP_STAGE scripting define symbol set - talks to the
                // server running on the VPS under the stage container/URL.
        Prod    // Device build with no environment define symbol set (the default) - talks to
                // the server running on the VPS under the prod container/URL. Deliberately the
                // "do nothing extra" case, so a build never ships pointed at stage by accident.
    }

    /// <summary>
    /// Single source of truth for which backend a build talks to. The Editor is always Dev;
    /// for device builds, add the "PP_STAGE" Scripting Define Symbol (Player Settings -> Other
    /// Settings -> Scripting Define Symbols) to produce a Stage build, or leave it unset for Prod.
    /// </summary>
    public static class EnvironmentConfig
    {
        private const string DevServerUrl = "http://localhost:5136";
        private const string StageServerUrl = "https://stage.pp.slamdunkinteractive.com";
        private const string ProdServerUrl = "https://pp.slamdunkinteractive.com";

        public static AppEnvironment Current
        {
            get
            {
#if UNITY_EDITOR
                return AppEnvironment.Dev;
#elif PP_STAGE
                return AppEnvironment.Stage;
#else
                return AppEnvironment.Prod;
#endif
            }
        }

        public static string ServerBaseUrl => Current switch
        {
            AppEnvironment.Dev => DevServerUrl,
            AppEnvironment.Stage => StageServerUrl,
            AppEnvironment.Prod => ProdServerUrl,
            _ => ProdServerUrl
        };
    }
}
