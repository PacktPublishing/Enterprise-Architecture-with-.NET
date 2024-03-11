using MongoDB.Bson.Serialization.Attributes;

public class SalesPetal
{
    [BsonElement("price")]
    public MonetaryAmount? Price { get; set; }

    [BsonElement("weightInGrams")]
    public decimal? WeightInGrams { get; set; }
}

public class MonetaryAmount
{
    [BsonElement("value")]
    public decimal Value { get; set; }

    [BsonElement("monetaryUnit")]
    public string MonetaryUnit { get; set; }
}