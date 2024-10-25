namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;

[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public class HeaderPagoPAAttribute(string? caption, bool readOnly = false) : Attribute
{
    public string? Caption { get; internal set; } = caption;
    public string? Name { get; set; }
    public bool ReadOnly { get; internal set; } = readOnly;
    public Type? Type { get; set; }
    public short Order { get; set; } = 0;
    public XCellStyle Style { get; set; } = XCellStyle.None;
}

[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public class HeaderPagoPAExcelAttribute(string? caption, bool readOnly = false) : Attribute
{
    public string? Caption { get; internal set; } = caption;
    public string? Name { get; set; }
    public bool ReadOnly { get; internal set; } = readOnly;
    public Type? Type { get; set; }
    public short Order { get; set; } = 0;
    public XCellStyle Style { get; set; } = XCellStyle.None;
}