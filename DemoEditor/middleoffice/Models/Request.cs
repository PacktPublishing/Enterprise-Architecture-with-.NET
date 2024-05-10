using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DemoEditor.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MiddleOffice.Models;

public class Request
{
    [BsonId()]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonIgnore]
    public ObjectId TechnicalId { get; set; }

    [BsonElement("entityId")]
    [Required(ErrorMessage = "Business identifier of a template is mandatory")]
    public string EntityId { get; set; }

    // [BsonElement("title")]
    // public List<InternationalizedString> Title { get; set; } = new List<InternationalizedString>();

    [BsonElement("template")]
    public TemplateLink Template { get; set; }

    [BsonElement("inputsValues")]
    public List<InputValue> InputsValues { get; set; } = new List<InputValue>();

    [BsonElement("idDecision")]
    public string? IdDecision { get; set; }
}

public class TemplateLink : Link
{
    [BsonElement("fullEntity")]
    public Template? FullEntity { get; set; }
}

public class InputValue
{
    [BsonElement("fieldId")]
    [Required(ErrorMessage = "Field id is mandatory")]
    public string FieldId { get; set; }

    [BsonElement("value")]
    public string Value { get; set; } // TODO: a more sophisticated approach would be to declare this as an object and accept specific types of data for serialization
}