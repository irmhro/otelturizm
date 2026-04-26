using System.Globalization;

namespace otelturizmnew.Models.Paneller.Firma;

public class FirmaCreateReservationPageViewModel
{
    public FirmaPanelShellViewModel Shell { get; set; } = new();
    public FirmaReservationCreateModel Form { get; set; } = new();
    public string? HotelSearch { get; set; }
    public List<FirmaSelectOption> Hotels { get; set; } = new();
    public List<FirmaSelectOption> RoomTypes { get; set; } = new();
    public FirmaReservationPriceCompareViewModel Compare { get; set; } = new();
    public List<FirmaEmployeeOptionViewModel> Employees { get; set; } = new();
}

public class FirmaReservationCreateModel
{
    public long HotelId { get; set; }
    public long RoomTypeId { get; set; }
    public DateOnly CheckInDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
    public DateOnly CheckOutDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(9));
    public int RoomCount { get; set; } = 5;
    public int AdultCount { get; set; } = 2;
    public int ChildCount { get; set; }
    public long? EmployeeUserId { get; set; }
    public string? EmployeeEmailsCsv { get; set; }
    public string? Note { get; set; }
}

public class FirmaReservationPriceCompareViewModel
{
    public decimal StandardTotal { get; set; }
    public decimal CompanyTotal { get; set; }
    public decimal Savings => Math.Max(0m, StandardTotal - CompanyTotal);
    public string StandardTotalText { get; set; } = "-";
    public string CompanyTotalText { get; set; } = "-";
    public string SavingsText { get; set; } = "-";
    public string NightCountText { get; set; } = "-";
    public bool HasCompanyPrice { get; set; }
    public string Note { get; set; } = string.Empty;
}

public class FirmaEmployeeOptionViewModel
{
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}

public class FirmaSelectOption
{
    public long Value { get; set; }
    public string Label { get; set; } = string.Empty;
}

