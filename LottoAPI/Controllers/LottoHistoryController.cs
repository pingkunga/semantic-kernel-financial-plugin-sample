using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;

[ApiController]
[Route("api/[controller]")]
public class LottoController : ControllerBase
{
    private readonly LottoHistoryService _lottoHistoryService;
    private readonly ILogger<LottoController> _logger;

    public LottoController(LottoHistoryService lottoHistoryService, ILogger<LottoController> logger)
    {
        _lottoHistoryService = lottoHistoryService;
        _logger = logger;
    }

    [HttpGet("prize2digithistory")]
    public async Task<IActionResult> GetPrize2DigitHistory([FromQuery] int? month, [FromQuery] int? day)
    {
        // Validate month and day parameters
        if (day.HasValue && (day < 1 || day > 31))
        {
            _logger.LogWarning("Invalid day value: {Day}. Must be between 1 and 31.", day);
            return BadRequest("Day must be between 1 and 31.");
        }
    
        if (month.HasValue && (month < 1 || month > 12))
        {
            _logger.LogWarning("Invalid month value: {Month}. Must be between 1 and 12.", month);
            return BadRequest("Month must be between 1 and 12.");
        }

        try
        {
            _logger.LogInformation(
                "Retrieving prize 2 digit history with month filter: {Month}",
                month
            );
            var results = await _lottoHistoryService.GetPrize2DigitHistoryAsync(month, day);
            return Ok(results);
        }
        catch (ApplicationException ex)
        {
            _logger.LogError(ex, "Application error while retrieving prize 2 digit history.");
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving prize 2 digit history.");
            return StatusCode(500, "Internal server error while retrieving data.");
        }
    }

    /*
    [HttpGet("prize2digithistory")]
    public async Task<IActionResult> GetPrize2DigitHistory([FromQuery] int? month)
    {
        
        var collection = _database.GetCollection<BsonDocument>("lotto_history");

        if (month.HasValue)
        {
            var pipeline = new[]
            {
                // InvalidCastException: Unable to cast object of type 'MongoDB.Bson.BsonDateTime' to type 'MongoDB.Bson.BsonBoolean'.
                new BsonDocument(
                    "$match",
                    new BsonDocument("Date", new BsonDocument("$type", "date"))
                ),
                new BsonDocument(
                    "$addFields",
                    new BsonDocument("month", new BsonDocument("$month", "$Date"))
                ),
                new BsonDocument("$match", new BsonDocument("month", month.Value)),
                new BsonDocument(
                    "$project",
                    new BsonDocument
                    {
                        { "Date", 1 },
                        { "Prize2Digits", 1 },
                        // { "PrizeSub3Digits", 1 },
                        // { "PrizePre3Digit", 1 },
                        { "_id", 0 }
                    }
                )
            };
            var results = await collection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            var json = results.ToJson(); // Requires using MongoDB.Bson
            return Content(json, "application/json");
        }
        else
        {
            var results = await collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            var json = results.ToJson(); // Requires using MongoDB.Bson
            return Content(json, "application/json");
        }
    }
    */
}
