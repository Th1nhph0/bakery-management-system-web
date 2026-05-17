using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Bakery.Data.DTOs
{
    public class LoginDTO
    {
        public string Email { get; set; }
        public string MatKhau { get; set; }
    }
}
