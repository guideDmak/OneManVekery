namespace OneManVekery.ViewModel;

public class AdminDashboardViewModel
{
    public string DateRangeLabel { get; init; } = string.Empty;

    public IReadOnlyList<AdminMetricCardViewModel> Metrics { get; init; } = [];

    public IReadOnlyList<AdminChartPointViewModel> DashboardBars { get; init; } = [];

    public int CartRecoveryPercent { get; init; }

    public int AbandonedCartCount { get; init; }

    public decimal AbandonedRevenue { get; init; }

    public IReadOnlyList<AdminDeviceRevenueViewModel> DeviceRevenue { get; init; } = [];

    public int StoreVisits { get; init; }

    public int Visitors { get; init; }

    public IReadOnlyList<AdminChartPointViewModel> TrafficLine { get; init; } = [];

    public IReadOnlyList<AdminBestsellerViewModel> Bestsellers { get; init; } = [];

    public IReadOnlyList<AdminForecastCardViewModel> ForecastCards { get; init; } = [];

    public IReadOnlyList<AdminLatestOrderViewModel> LatestOrders { get; init; } = [];
}

public class AdminOrdersViewModel
{
    public string DateRangeLabel { get; init; } = string.Empty;

    public IReadOnlyList<AdminMetricCardViewModel> Metrics { get; init; } = [];

    public string UpdateLabel { get; init; } = string.Empty;

    public string UpdateValue { get; init; } = string.Empty;

    public string UpdateDelta { get; init; } = string.Empty;

    public IReadOnlyList<AdminChartPointViewModel> UpdateChart { get; init; } = [];

    public IReadOnlyList<AdminInfoItemViewModel> FulfillmentSummary { get; init; } = [];

    public IReadOnlyList<AdminLatestOrderViewModel> Orders { get; init; } = [];
}

public class AdminCustomersViewModel
{
    public string DateRangeLabel { get; init; } = string.Empty;

    public IReadOnlyList<AdminMetricCardViewModel> Metrics { get; init; } = [];

    public string CustomerName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Segment { get; init; } = string.Empty;

    public IReadOnlyList<AdminInfoItemViewModel> SummaryItems { get; init; } = [];

    public string RevenueLabel { get; init; } = string.Empty;

    public string RevenueValue { get; init; } = string.Empty;

    public string RevenueDelta { get; init; } = string.Empty;

    public IReadOnlyList<AdminChartPointViewModel> RevenueChart { get; init; } = [];

    public IReadOnlyList<AdminLatestOrderViewModel> Orders { get; init; } = [];
}

public class AdminProductsViewModel
{
    public string DateRangeLabel { get; init; } = string.Empty;

    public IReadOnlyList<AdminMetricCardViewModel> Metrics { get; init; } = [];

    public string ProductName { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string ProductCaption { get; init; } = string.Empty;

    public IReadOnlyList<AdminInfoItemViewModel> SummaryItems { get; init; } = [];

    public string RevenueLabel { get; init; } = string.Empty;

    public string RevenueValue { get; init; } = string.Empty;

    public string RevenueDelta { get; init; } = string.Empty;

    public IReadOnlyList<AdminChartPointViewModel> RevenueChart { get; init; } = [];

    public IReadOnlyList<AdminLatestOrderViewModel> Orders { get; init; } = [];
}

public class AdminReportsViewModel
{
    public IReadOnlyList<AdminCampaignSectionViewModel> Sections { get; init; } = [];

    public IReadOnlyList<AdminCampaignRuleViewModel> Rules { get; init; } = [];
}

public class AdminCampaignSectionViewModel
{
    public string StepNumber { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public IReadOnlyList<AdminInfoItemViewModel> Items { get; init; } = [];
}

public class AdminCampaignRuleViewModel
{
    public string Metric { get; init; } = string.Empty;

    public string IntroLabel { get; init; } = string.Empty;

    public string Comparison { get; init; } = string.Empty;

    public string Threshold { get; init; } = string.Empty;

    public string MidLabel { get; init; } = string.Empty;

    public string ActionLabel { get; init; } = string.Empty;

    public string ActionValue { get; init; } = string.Empty;
}

public class AdminInfoItemViewModel
{
    public string Label { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public string Detail { get; init; } = string.Empty;

    public string AccentKey { get; init; } = string.Empty;
}

public class AdminMetricCardViewModel
{
    public string Label { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public string Delta { get; init; } = string.Empty;

    public bool PositiveTrend { get; init; }

    public string AccentKey { get; init; } = string.Empty;
}

public class AdminChartPointViewModel
{
    public string Label { get; init; } = string.Empty;

    public int Value { get; init; }

    public bool IsHighlighted { get; init; }
}

public class AdminDeviceRevenueViewModel
{
    public string Label { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public string Share { get; init; } = string.Empty;

    public string AccentKey { get; init; } = string.Empty;
}

public class AdminBestsellerViewModel
{
    public string Product { get; init; } = string.Empty;

    public string Price { get; init; } = string.Empty;

    public string Sold { get; init; } = string.Empty;

    public string Profit { get; init; } = string.Empty;
}

public class AdminForecastCardViewModel
{
    public string Label { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public string Delta { get; init; } = string.Empty;

    public bool PositiveTrend { get; init; }

    public string AccentKey { get; init; } = string.Empty;
}

public class AdminLatestOrderViewModel
{
    public string OrderId { get; init; } = string.Empty;

    public string Product { get; init; } = string.Empty;

    public string SecondaryText { get; init; } = string.Empty;

    public string Customer { get; init; } = string.Empty;

    public string Quantity { get; init; } = string.Empty;

    public string Date { get; init; } = string.Empty;

    public string Revenue { get; init; } = string.Empty;

    public string NetProfit { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;
}
