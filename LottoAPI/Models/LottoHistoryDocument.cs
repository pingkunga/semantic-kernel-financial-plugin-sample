using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class LottoHistoryDocument
{
    [BsonId]
    public ObjectId Id { get; set; }
    public DateTime? Date { get; set; }
    public string Prize1st { get; set; }
    public string[] PrizePre3Digit { get; set; }
    public string[] PrizeSub3Digits { get; set; }
    public string Prize2Digits { get; set; }
    public string[] Nearby1st { get; set; }
    public string[] Prize2nd { get; set; }
    public string[] Prize3rd { get; set; }
    public string[] Prize4th { get; set; }
    public string[] Prize5th { get; set; }
}