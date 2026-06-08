namespace otelturizmnew.Models.Paneller.Admin;

public class AdminHomepageHotelsPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminHomepageHotelSectionViewModel> Sections { get; set; } = new();
    public AdminHomepageSectionForm SectionForm { get; set; } = new();
    public long? ActiveSectionId { get; set; }
    public string SearchTerm { get; set; } = string.Empty;
}

public class AdminHomepageHotelSectionViewModel
{
    public long Id { get; set; }
    public string SectionCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystemSection => string.Equals(SectionCode, "ozel-rotalar", StringComparison.OrdinalIgnoreCase);
    public List<AdminHomepageHotelEntryViewModel> Hotels { get; set; } = new();
}

public class AdminHomepageHotelEntryViewModel
{
    public long EntryId { get; set; }
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public string PublishStatus { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
}

public class AdminHomepageHotelSearchResultViewModel
{
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string LocationLabel { get; set; } = string.Empty;
}

public class AdminHomepageSectionForm
{
    public long? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public int SortOrder { get; set; } = 100;
    public bool IsActive { get; set; } = true;
}

public class AdminHomepageAddHotelForm
{
    public long SectionId { get; set; }
    public long HotelId { get; set; }
}

public class AdminHomepageRemoveHotelForm
{
    public long EntryId { get; set; }
    public long SectionId { get; set; }
}

public class AdminHomepageReorderHotelsForm
{
    public long SectionId { get; set; }
    public string EntryIds { get; set; } = string.Empty;
}

public class AdminHomepageMoveHotelForm
{
    public long EntryId { get; set; }
    public long SectionId { get; set; }
    public string Direction { get; set; } = string.Empty;
}
