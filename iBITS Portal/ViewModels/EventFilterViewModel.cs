namespace iBITS_Portal.ViewModels
{
    public class EventFilterViewModel
    {
        public string? Search { get; set; }
        public string? AcadYear { get; set; }
        public string? EventType { get; set; }
        public string? Status { get; set; }
        public DateOnly? DateFrom { get; set; }
        public DateOnly? DateTo { get; set; }
        public bool? IsClosed { get; set; }
    }
}
