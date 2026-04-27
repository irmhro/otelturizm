using System.Globalization;

namespace otelturizmnew.Models.Paneller.Partner;

public class PartnerCompanyPricingPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public long HotelId { get; set; }
    public string? Warning { get; set; }
    public long? SelectedCompanyId { get; set; }
    public string SelectedCompanyName { get; set; } = string.Empty;
    public long? SelectedRoomId { get; set; }
    public DateOnly MonthAnchor { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    public List<PartnerCompanyOptionViewModel> Companies { get; set; } = new();
    public List<PartnerCompanyRoomOptionViewModel> Rooms { get; set; } = new();
    public List<PartnerCompanyPricingDayViewModel> Days { get; set; } = new();
    public PartnerCompanyBulkPricingUpdateRequest BulkForm { get; set; } = new();
    public string PreviousMonthQuery { get; set; } = string.Empty;
    public string NextMonthQuery { get; set; } = string.Empty;
}

public class PartnerCompanyOptionViewModel
{
    public long CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}

public class PartnerCompanyPricingDayViewModel
{
    public DateOnly Date { get; set; }
    public string DateText { get; set; } = string.Empty;
    public decimal? CompanyPrice { get; set; }
    public decimal? BasePrice { get; set; }
    public bool IsClosed { get; set; }
    public string CompanyPriceText { get; set; } = "-";
    public string BasePriceText { get; set; } = "-";
}

public class PartnerCompanyRoomOptionViewModel
{
    public long RoomTypeId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}

public class PartnerCompanyBulkPricingUpdateRequest
{
    public long HotelId { get; set; }
    public long CompanyId { get; set; }
    public long RoomTypeId { get; set; }
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(14));
    public decimal CompanyNightlyPrice { get; set; }
    public bool CloseSales { get; set; }
    public string? Note { get; set; }
}

