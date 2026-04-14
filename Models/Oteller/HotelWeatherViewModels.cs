namespace otelturizmnew.Models.Oteller;

public class HotelWeatherWidgetViewModel
{
    public string LocationLabel { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<HotelWeatherDayViewModel> Days { get; set; } = new();
}

public class HotelWeatherDayViewModel
{
    public string PeriodLabel { get; set; } = string.Empty;
    public string DateLabel { get; set; } = string.Empty;
    public string ConditionText { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-cloud-sun";
    public string TemperatureText { get; set; } = string.Empty;
    public string PrecipitationText { get; set; } = string.Empty;
}
