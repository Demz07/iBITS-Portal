using System;
using System.Collections.Generic;

namespace iBITS_Portal.ViewModels
{
    public class FineBatchViewModel
    {
        public string FineReason { get; set; }
        public string BatchId { get; set; }
        public DateTime? DateCreated { get; set; } // Optional for fines
        public int StudentCount { get; set; }
        public decimal TotalExpected { get; set; }
        public decimal TotalCollected { get; set; }

        // NEW PROPERTIES
        public List<string> AffectedPrograms { get; set; } = new List<string>();
        public List<string> AffectedYearLevels { get; set; } = new List<string>();

        public int CollectionRate => TotalExpected > 0 ? (int)((TotalCollected / TotalExpected) * 100) : 0;
    }
}