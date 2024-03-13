using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using WmiLight;

namespace GameFinder.StoreHandlers.EADesktop.Crypto.Windows;

[SupportedOSPlatform("windows")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[ExcludeFromCodeCoverage(Justification = "Only available on Windows.")]
internal static class WMIHelper
{
    // private const string ComputerName = "localhost";
    // private const string Namespace = @"ROOT\CIMV2";
    // private const string QueryDialect = "WQL";

    public const string Win32BaseBoardClass = "Win32_BaseBoard";
    public const string Win32BIOSClass = "Win32_BIOS";
    public const string Win32VideoControllerClass = "Win32_VideoController";
    public const string Win32ProcessorClass = "Win32_Processor";

    public const string ManufacturerPropertyName = "Manufacturer";
    public const string SerialNumberPropertyName = "SerialNumber";
    public const string PNPDeviceIDPropertyName = "PNPDeviceId";
    public const string NamePropertyName = "Name";
    public const string ProcessorIDPropertyName = "ProcessorId";

    public static string GetWMIProperty(string className, string propertyName)
    {
        var query = $"SELECT {propertyName} FROM {className}";

        using var con = new WmiConnection();
        foreach (var obj in con.CreateQuery(query))
        {
            return obj[propertyName].ToString() ?? "";
        }

        return "";
    }
}
