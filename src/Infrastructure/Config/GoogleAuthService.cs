using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using ActivitiesTracker.Domain.Contracts;

namespace ActivitiesTracker.Infrastructure.Config;

public sealed class GoogleAuthService
{
    private readonly ILogSink _log;
    private static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };

    public GoogleAuthService(ILogSink log)
    {
        _log = log;
    }

    public async Task<UserCredential?> GetCredentialAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(AppPaths.CredentialsPath))
            {
                _log.Error($"Credentials file not found at {AppPaths.CredentialsPath}. User must provide this file.");
                return null;
            }

            using var stream = new FileStream(AppPaths.CredentialsPath, FileMode.Open, FileAccess.Read);
            var tokenPath = Path.Combine(AppPaths.AppDataRoot, "token.json");

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                Scopes,
                "user",
                cancellationToken,
                new FileDataStore(tokenPath, true));

            _log.Info("Google OAuth credential successfully loaded or created.");
            return credential;
        }
        catch (Exception ex)
        {
            _log.Error("Failed to initialize Google OAuth.", ex);
            return null;
        }
    }
}
