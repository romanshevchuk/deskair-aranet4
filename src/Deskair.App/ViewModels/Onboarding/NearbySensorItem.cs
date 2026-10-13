using Deskair.Domain.Readings;

namespace Deskair.App.ViewModels.Onboarding;

/// <summary>A sensor in the "found nearby" list. The detail text (signal) is already localized.</summary>
public sealed record NearbySensorItem(DeviceId Id, string Name, string Detail);
