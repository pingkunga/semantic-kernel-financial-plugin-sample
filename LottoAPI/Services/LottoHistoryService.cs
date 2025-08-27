using MongoDB.Bson;
using MongoDB.Driver;

public class LottoHistoryService
{
    private readonly IMongoCollection<LottoHistoryDocument> _collection;
    private readonly ILogger<LottoHistoryService> _logger;

    public LottoHistoryService(IMongoDatabase database, ILogger<LottoHistoryService> logger)
    {
        _collection = database.GetCollection<LottoHistoryDocument>("lotto_history");
        _logger = logger;
    }

    public async Task<List<LottoHistoryDocument>> GetAllAsync()
    {
        _logger.LogInformation("Retrieving all lotto history records.");
        return await _collection.Find(FilterDefinition<LottoHistoryDocument>.Empty).ToListAsync();
    }

    public async Task<LottoHistoryDocument> GetByIdAsync(ObjectId id)
    {
        _logger.LogInformation($"Retrieving lotto history record with ID: {id}");
        return await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task CreateAsync(LottoHistoryDocument history)
    {
        _logger.LogInformation("Creating a new lotto history record.");
        await _collection.InsertOneAsync(history);
    }

    public async Task UpdateAsync(ObjectId id, LottoHistoryDocument history)
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

            /*
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
                        new BsonDocument(
                            "$match",
                            new BsonDocument("day", new BsonDocument("$gte", 1).Add("$lte", 15))
                        )
                    );
                }
                else
                {
                    pipeline.Add(
                        new BsonDocument(
                            "$match",
                            new BsonDocument("day", new BsonDocument("$gt", 15))
                        )
                    );
                }
            }
            */
            var pipeline = GetLottoBaseQuery(month, day);
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
                .OrderByDescending(x => x.Count)
                .ToList();
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

    public async Task<List<LottoPriceDigitHistory>> GetPrize3DigitHistoryAsync(int? month, int? day)
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
                month: 12
                }
            },
            {
                $facet: {
                PrizeSub3Digits: [
                    { $group: { _id: "$PrizeSub3Digits", count: { $sum: 1 } } },
                    { $sort: { count: -1 } }
                ],
                PrizePre3Digit: [
                    { $group: { _id: "$PrizePre3Digit", count: { $sum: 1 } } },
                    { $sort: { count: -1 } }
                ]
                }
            }
            ])
        */

        /*
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
        { $project: { PrizeSub3Digits: 1, PrizePre3Digit: 1, _id: 0 } }
        ])
        */

        //Map to Model LottoHistory
        try
        {
            var pipeline = GetLottoBaseQuery(month, day);
            pipeline.Add(
                // $project: เลือกเฉพาะฟิลด์ที่ต้องการ
                new BsonDocument(
                    "$project",
                    new BsonDocument
                    {
                        { "PrizeSub3Digits", 1 },
                        { "PrizePre3Digit", 1 },
                        { "_id", 0 }
                    }
                )
            );

            IList<LottoHistoryDocument> results = await _collection
                .Aggregate<LottoHistoryDocument>(pipeline)
                .ToListAsync();

            //Map to LottoPriceDigitHistory
            IList<LottoPriceDigitHistory> sub3DigitHistory = results
                .SelectMany(x => x.PrizeSub3Digits ?? Array.Empty<string>())
                .GroupBy(x => x)
                .Select(g => new LottoPriceDigitHistory { PrizeDigits = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            IList<LottoPriceDigitHistory> pre3DigitHistory = results
                .SelectMany(x => x.PrizePre3Digit ?? Array.Empty<string>())
                .GroupBy(x => x)
                .Select(g => new LottoPriceDigitHistory { PrizeDigits = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            //Merge sub3DigitHistory and pre3DigitHistory and Count on same PrizeDigits
            return sub3DigitHistory
                .Concat(pre3DigitHistory)
                .GroupBy(x => x.PrizeDigits)
                .Select(
                    g =>
                        new LottoPriceDigitHistory
                        {
                            PrizeDigits = g.Key,
                            Count = g.Sum(x => x.Count)
                        }
                )
                .OrderByDescending(x => x.Count)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving prize 3 digit history.");
            throw new ApplicationException(
                "Error retrieving prize 3 digit history from database.",
                ex
            );
        }
    }

    private List<BsonDocument> GetLottoBaseQuery(int? month, int? day)
    {
        var query = new List<BsonDocument>
        {
            new BsonDocument("$match", new BsonDocument("Date", new BsonDocument("$type", "date"))),
            new BsonDocument(
                "$addFields",
                new BsonDocument("month", new BsonDocument("$month", "$Date"))
            ),
            new BsonDocument(
                "$addFields",
                new BsonDocument("day", new BsonDocument("$dayOfMonth", "$Date"))
            )
        };

        bool IsFirstHalfOfMonth = day.HasValue && day.Value >= 1 && day.Value <= 15;

        if (month.HasValue)
        {
            query.Add(new BsonDocument("$match", new BsonDocument("month", month.Value)));

            if (IsFirstHalfOfMonth)
            {
                query.Add(
                    new BsonDocument(
                        "$match",
                        new BsonDocument("day", new BsonDocument("$gte", 1).Add("$lte", 15))
                    )
                );
            }
            else
            {
                query.Add(
                    new BsonDocument("$match", new BsonDocument("day", new BsonDocument("$gt", 15)))
                );
            }
        }
        return query;
    }
}
