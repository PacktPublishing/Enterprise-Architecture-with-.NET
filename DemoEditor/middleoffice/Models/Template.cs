using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DemoEditor.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MiddleOffice.Models;

public class Template
{
    [BsonId()]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonIgnore]
    public ObjectId TechnicalId { get; set; }

    [BsonElement("entityId")]
    [Required(ErrorMessage = "Business identifier of a template is mandatory")]
    public string EntityId { get; set; }

    [BsonElement("title")]
    public List<InternationalizedString> Title { get; set; } = new List<InternationalizedString>();

    [BsonElement("fields")]
    public List<Field> Fields { get; set; } = new List<Field>();

    [BsonElement("possibleDecisions")]
    public List<Decision> PossibleDecisions { get; set; } = new List<Decision>();

    [BsonElement("status")]
    public string Status { get; set; } = "active";
}

public class Field
{
    [BsonElement("id")]
    [Required(ErrorMessage = "Field id is mandatory")]
    public string Id { get; set; }

    [BsonElement("name")]
    public List<InternationalizedString> Name { get; set; } = new List<InternationalizedString>();

    [BsonElement("inputType")]
    public string InputType { get; set; }

    [BsonElement("required")]
    public bool Required { get; set; } = true;

    [BsonElement("visible")]
    public bool Visible { get; set; } = true;
}