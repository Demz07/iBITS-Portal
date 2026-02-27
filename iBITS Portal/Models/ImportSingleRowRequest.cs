namespace iBITS_Portal.Models
{
    public class ImportSingleRowRequest
    {
        public int RowNum { get; set; }
        public string? StudentNum { get; set; }
        public string? StudentLn { get; set; }
        public string? StudentFn { get; set; }
        public string? StudentMn { get; set; }
        public string? YearLevelSection { get; set; }
        public string? Year { get; set; }
        public string? Section { get; set; }
        public string? Course { get; set; }
        public string? StudentEmail { get; set; }
        public string? StudentType { get; set; }
        public string? Birthday { get; set; }
        public string? DateFormat { get; set; }
    }
}
