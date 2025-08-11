namespace ContactService.ApiContract.Response.Queries
{
    public class LocationStatisticsResponse
    {
        public string Location { get; set; } = string.Empty;
        public int ContactCount { get; set; }
        public int PhoneNumberCount { get; set; }
    }
}
