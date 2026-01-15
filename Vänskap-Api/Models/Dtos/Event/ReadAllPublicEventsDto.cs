namespace Vänskap_Api.Models.Dtos.Event
{
    public class ReadAllPublicEventsDto
    {
        public List<string?>? Interests { get; set; }
        public int? AgeMax { get; set; }
        public int? AgeMin { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string Sort { get; set; } = "alphabetical";
    }
}
