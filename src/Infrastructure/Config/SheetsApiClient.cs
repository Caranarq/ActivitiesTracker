using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using ActivitiesTracker.Domain.Contracts;

namespace ActivitiesTracker.Infrastructure.Config;

public sealed class SheetsApiClient
{
    private readonly SheetsService _service;
    private readonly ILogSink _log;

    public SheetsApiClient(UserCredential credential, ILogSink log)
    {
        _log = log;
        _service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "ActivitiesTracker"
        });
    }

    public async Task<IList<IList<object>>> ReadRangeAsync(string spreadsheetId, string range, CancellationToken cancellationToken = default)
    {
        var request = _service.Spreadsheets.Values.Get(spreadsheetId, range);
        var response = await request.ExecuteAsync(cancellationToken);
        return response.Values ?? new List<IList<object>>();
    }

    public async Task AppendRowAsync(string spreadsheetId, string range, IList<object> values, CancellationToken cancellationToken = default)
    {
        var valueRange = new ValueRange { Values = new List<IList<object>> { values } };
        var request = _service.Spreadsheets.Values.Append(valueRange, spreadsheetId, range);
        request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
        await request.ExecuteAsync(cancellationToken);
    }

    public async Task UpdateRowAsync(string spreadsheetId, string range, IList<object> values, CancellationToken cancellationToken = default)
    {
        var valueRange = new ValueRange { Values = new List<IList<object>> { values } };
        var request = _service.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
        request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
        await request.ExecuteAsync(cancellationToken);
    }

    public async Task<Spreadsheet> GetSpreadsheetAsync(string spreadsheetId, CancellationToken cancellationToken = default)
    {
        var request = _service.Spreadsheets.Get(spreadsheetId);
        return await request.ExecuteAsync(cancellationToken);
    }
    
    public async Task BatchUpdateAsync(string spreadsheetId, BatchUpdateSpreadsheetRequest requestBody, CancellationToken cancellationToken = default)
    {
        var request = _service.Spreadsheets.BatchUpdate(requestBody, spreadsheetId);
        await request.ExecuteAsync(cancellationToken);
    }

    public async Task<Spreadsheet> CreateSpreadsheetAsync(string title, CancellationToken cancellationToken = default)
    {
        var spreadsheet = new Spreadsheet
        {
            Properties = new SpreadsheetProperties { Title = title }
        };
        var request = _service.Spreadsheets.Create(spreadsheet);
        return await request.ExecuteAsync(cancellationToken);
    }
}
