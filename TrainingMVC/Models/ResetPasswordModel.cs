using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TrainingMVC.Models
{
    public class ResetPasswordModel
    {
        
        [Required(AllowEmptyStrings = false, ErrorMessage ="New password is required")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Compare("NewPassword",ErrorMessage ="Password don't match")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
                
        [Required]
        public string ResetCode { get; set; }
    }
}