using MongoDB.Bson;
using MongoDB.Driver;

public class LottoHistoryService
{
    private readonly IMongoCollection<LottoHistory> _collection;
    private readonly ILogger<LottoHistoryService> _logger;
    public LottoHistoryService(IMongoDatabase database, ILogger<LottoHistoryService> logger)
    {
        _collection = database.GetCollection<LottoHistory>("lotto_history");
        _logger = logger;
    }

    public async Task<List<LottoHistory>> GetAllAsync()
    {
        _logger.LogInformation("Retrieving all lotto history records.");
        return await _collection.Find(FilterDefinition<LottoHistory>.Empty).ToListAsync();
    }

    public async Task<LottoHistory> GetByIdAsync(ObjectId id)
    {
        _logger.LogInformation($"Retrieving lotto history record with ID: {id}");
        return await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task CreateAsync(LottoHistory history)
    {
        _logger.LogInformation("Creating a new lotto history record.");
        await _collection.InsertOneAsync(history);
    }

    public async Task UpdateAsync(ObjectId id, LottoHistory history)
    {
        _logger.LogInformation($"Updating lotto history record with ID: {id}");
        await _collection.ReplaceOneAsync(x => x.Id == id, history);
    }

    public async Task DeleteAsync(ObjectId id)
    {
        _logger.LogInformation($"Deleting lotto history record with ID: {id}");
        await _collection.DeleteOneAsync(x => x.Id == id);
    }

    public async Task<List<LottoPriceDigitHistory>> GetPrize2DigitHistoryAsync(int? month, int? day)
    {
        /*
        db["lotto_history"].aggregate([
        {
            $addFields: {
            month: { $month: "$Date" }
            }
        },
        {
            $match: {
            month: 12,
            Prize2Digits: { $exists: true, $ne: null }
            }
        },
        {
            $group: {
            _id: "$Prize2Digits",
            count: { $sum: 1 }
            }
        }
        ])

        // ตามงวด 1-15
        db["lotto_history"].aggregate([
        {
            $addFields: {
            month: { $month: "$Date" },
            day: { $dayOfMonth: "$Date" }
            }
        },
        {
            $match: {
            month: 12,
            day: { $gte: 1, $lte: 15 },
            Prize2Digits: { $exists: true, $ne: null }
            }
        },
        {
            $group: {
            _id: "$Prize2Digits",
            count: { $sum: 1 }
            }
        }
        ])
        */

        try
        {
            _logger.LogInformation("Retrieving prize 2 digit history with optional month filter.");

            bool IsFirstHalfOfMonth = day.HasValue && day.Value >= 1 && day.Value <= 15;

            var pipeline = new List<BsonDocument>
            {
                new BsonDocument(
                    "$match",
                    new BsonDocument("Date", new BsonDocument("$type", "date"))
                ),
                new BsonDocument(
                    "$addFields",
                    new BsonDocument("month", new BsonDocument("$month", "$Date"))
                ),

                new BsonDocument(
                    "$addFields",
                    new BsonDocument("day", new BsonDocument("$dayOfMonth", "$Date"))
                )
            };

            if (month.HasValue)
            {
                pipeline.Add(new BsonDocument("$match", new BsonDocument("month", month.Value)));

                if (IsFirstHalfOfMonth)
                {
                    pipeline.Add(
                        new BsonDocument("$match", new BsonDocument("day", new BsonDocument("$gte", 1).Add("$lte", 15)))
                    );
                }
                else
                {
                    pipeline.Add(
                        new BsonDocument("$match", new BsonDocument("day", new BsonDocument("$gt", 15)))
                    );
                }
            }

            pipeline.Add(
                new BsonDocument(
                    "$group",
                    new BsonDocument
                    {
                        { "_id", "$Prize2Digits" },
                        { "Count", new BsonDocument("$sum", 1) }
                    }
                )
            );

            var results = await _collection.Aggregate<BsonDocument>(pipeline).ToListAsync();

            return results
                .Select(
                    r =>
                        new LottoPriceDigitHistory
                        {
                            PrizeDigits = r["_id"].AsString,
                            Count = r["Count"].AsInt32
                        }
                )
                .OrderBy(x => x.Count).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving prize 2 digit history.");
            throw new ApplicationException(
                "Error retrieving prize 2 digit history from database.",
                ex
            );
        }
    }
}
