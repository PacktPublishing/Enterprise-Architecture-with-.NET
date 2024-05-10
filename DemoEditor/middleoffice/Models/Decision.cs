using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DemoEditor.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MiddleOffice.Models;

public class Decision
{
    [BsonElement("id")]
    [Required(ErrorMessage = "Field id is mandatory")]
    public string Id { get; set; }

    [BsonElement("title")]
    public List<InternationalizedString> Title { get; set; } = new List<InternationalizedString>();

    [BsonElement("actions")]
    public List<Action> Actions { get; set; } = new List<Action>();
}

public class Action
{
    [BsonElement("href")]
    public string Href { get; set; }

    [BsonElement("verb")]
    public string Verb { get; set; }
}