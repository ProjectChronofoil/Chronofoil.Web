using System.Globalization;
using System.Text.Json;
using Chronofoil.Common;
using Chronofoil.Common.Info;

namespace Chronofoil.Web.Services.Info;

public class InfoService : IInfoService
{
    private readonly FaqResponse _faq;
    private readonly List<TosResponse> _tosRegistry;

    public InfoService()
    {
        _faq = JsonSerializer.Deserialize<FaqResponse>(File.ReadAllText("Data/faq.json"))!;
        
        _tosRegistry = new List<TosResponse>
        {
            new() {
                Version = 1,
                EnactedDate = DateTime.Parse("2024-06-01", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                Text = File.ReadAllText("Data/tos1.txt"), 
            },
        };
    }
    
    public ApiResult<FaqResponse> GetCurrentFaq()
    {
        return ApiResult<FaqResponse>.Success(_faq);
    } 

    public ApiResult<TosResponse> GetCurrentTos()
    {
        var currentDate = DateTime.UtcNow;
        var last = _tosRegistry.FindLast(tos => tos.EnactedDate <= currentDate);
        if (last == null) throw new Exception("lol idk");
        return new ApiResult<TosResponse>(last);
    }
}