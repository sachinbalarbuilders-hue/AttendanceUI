using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AttendanceUI.Models;

public class LoanType
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("type_name")]
    [Required]
    [StringLength(100)]
    public string TypeName { get; set; } = "";

    [Column("max_amount")]
    public decimal? MaxAmount { get; set; }

    [Column("max_installments")]
    public int? MaxInstallments { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
