using System.ComponentModel.DataAnnotations;

namespace StarLedger.Api.Dtos.Resource;

public class UpdateResourceRequest
{
    [StringLength(100)]
    public required string Name { get; set; }
    [StringLength(4, ErrorMessage = "Code can only be 4 digits long")]
    public required string Code { get; set; }
    [StringLength(50)]
    public required string Type { get; set; }
    public double PriceBuy { get; set; }
    public double PriceSell { get; set; }
}